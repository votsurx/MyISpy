import socket
import struct
import sys
from collections import deque
from ultralytics import YOLO
from PIL import Image
import io
import time
import traceback

# Парсим аргументы
port = 5003
confidence = 0.5

for i, arg in enumerate(sys.argv):
    if arg == "--port":
        port = int(sys.argv[i + 1])
    elif arg == "--conf":
        confidence = float(sys.argv[i + 1])

print(f"YOLO TCP сервер: порт={port}, conf={confidence}", flush=True)

# Буфер
BUFFER_SIZE = 12
buffer = deque(maxlen=BUFFER_SIZE)
frame_count = 0
last_cleanup = time.time()
BUFFER_TIMEOUT = 3.0  # Удаляем кадры старше 3 секунд

# Модель
print("YOLO: загрузка модели...", flush=True)
try:
    model = YOLO("yolov8n.onnx")
    print("YOLO: модель загружена", flush=True)
except Exception as e:
    print(f"YOLO: ОШИБКА загрузки модели - {e}", flush=True)
    sys.exit(1)

# Создаём TCP сервер
server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
server.bind(('localhost', port))
server.listen(1)

print(f"YOLO: ожидание подключения на порту {port}...", flush=True)
conn, addr = server.accept()
print(f"YOLO: подключён {addr}", flush=True)

def cleanup_buffer():
    """Удаляет старые кадры из буфера"""
    global last_cleanup
    try:
        current_time = time.time()
        
        if current_time - last_cleanup > BUFFER_TIMEOUT:
            removed = 0
            while len(buffer) > 6:
                buffer.popleft()
                removed += 1
            if removed > 0:
                print(f"YOLO: очистка буфера - удалено {removed} старых кадров, осталось {len(buffer)}", flush=True)
            last_cleanup = current_time
    except Exception as e:
        print(f"YOLO: ошибка очистки буфера - {e}", flush=True)

def process_buffer():
    """Проверяет буфер и ОЧИЩАЕТ его"""
    try:
        if len(buffer) == 0:
            print(f"YOLO: буфер пуст!", flush=True)
            return "SKIP"
        
        print(f"YOLO: проверка {len(buffer)} кадров...", flush=True)
        
        frames_checked = 0
        
        while buffer:
            try:
                img = buffer.popleft()
            except IndexError:
                break
                
            frames_checked += 1
            
            try:
                results = model(img, verbose=False, conf=confidence)
                
                if results and len(results) > 0 and len(results[0].boxes) > 0:
                    classes = []
                    confidences = []
                    
                    for box in results[0].boxes:
                        try:
                            cls_id = int(box.cls)
                            if cls_id in model.names:
                                classes.append(model.names[cls_id])
                                confidences.append(float(box.conf))
                        except Exception:
                            continue
                    
                    if classes:
                        print(f"YOLO: НАЙДЕНЫ ОБЪЕКТЫ:", flush=True)
                        for cls, conf_val in zip(classes, confidences):
                            print(f"  - {cls}: {conf_val:.2%}", flush=True)
                        
                        buffer.clear()
                        return "RECORD"
                    
            except Exception as e:
                print(f"YOLO: ошибка обработки кадра - {e}", flush=True)
                traceback.print_exc()
                continue
        
        print(f"YOLO: проверено {frames_checked} кадров, объекты не найдены", flush=True)
        return "SKIP"
        
    except Exception as e:
        print(f"YOLO: КРИТИЧЕСКАЯ ошибка в process_buffer - {e}", flush=True)
        traceback.print_exc()
        return "SKIP"

def recv_all(sock, n):
    """Получает ровно n байт из сокета"""
    data = bytearray()
    while len(data) < n:
        try:
            packet = sock.recv(n - len(data))
            if not packet:
                return None
            data.extend(packet)
        except socket.timeout:
            return None
        except Exception as e:
            print(f"YOLO: ошибка получения данных - {e}", flush=True)
            return None
    return data

print("YOLO: готов к работе", flush=True)

# Устанавливаем таймаут на сокет
conn.settimeout(1.0)

while True:
    try:
        # Периодическая очистка буфера
        cleanup_buffer()
        
        # Читаем длину пакета (4 байта)
        raw_len = recv_all(conn, 4)
        if not raw_len:
            continue
            
        length = struct.unpack(">I", raw_len)[0]
        
        # Проверка на слишком большой пакет
        if length > 10 * 1024 * 1024:  # 10 МБ максимум
            print(f"YOLO: пакет слишком большой ({length} байт), пропускаем", flush=True)
            continue
        
        # Читаем данные
        data = recv_all(conn, length)
        if not data:
            continue
        
        # Обрабатываем команду CHECK
        if data == b"CHECK":
            try:
                response = process_buffer()
                conn.sendall(response.encode())
                print(f"YOLO: CHECK -> {response}", flush=True)
            except Exception as e:
                print(f"YOLO: ошибка отправки ответа CHECK - {e}", flush=True)
                try:
                    conn.sendall(b"SKIP")
                except:
                    break
                    
        # Обрабатываем команду CONF
        elif data.startswith(b"CONF:"):
            try:
                new_conf = float(data[5:])
                if 0.01 <= new_conf <= 1.0:
                    confidence = new_conf
                    print(f"YOLO: порог изменён на {confidence}", flush=True)
                else:
                    print(f"YOLO: некорректный порог: {new_conf}", flush=True)
            except ValueError:
                print(f"YOLO: ошибка парсинга порога", flush=True)
            
        # Обрабатываем JPEG кадр
        else:
            try:
                img = Image.open(io.BytesIO(data))
                img = img.convert("RGB")
                buffer.append(img)
                frame_count += 1
                
                if frame_count % 30 == 0:
                    print(f"YOLO: получено кадров: {frame_count}, в буфере: {len(buffer)}", flush=True)
                    
            except Exception as e:
                print(f"YOLO: ошибка декодирования кадра - {e}", flush=True)
                
    except socket.timeout:
        # Таймаут - нормально, продолжаем
        continue
    except ConnectionResetError:
        print("YOLO: соединение сброшено клиентом", flush=True)
        break
    except BrokenPipeError:
        print("YOLO: канал сломан", flush=True)
        break
    except ConnectionAbortedError:
        print("YOLO: соединение прервано", flush=True)
        break
    except OSError as e:
        print(f"YOLO: ошибка сокета - {e}", flush=True)
        break
    except Exception as e:
        print(f"YOLO: неожиданная ошибка в главном цикле - {e}", flush=True)
        traceback.print_exc()
        try:
            conn.sendall(b"SKIP")
        except:
            pass
        # Не выходим, продолжаем работу
        time.sleep(0.5)

print("YOLO: сервер остановлен", flush=True)

# Корректное завершение
try:
    conn.shutdown(socket.SHUT_RDWR)
except:
    pass
try:
    conn.close()
except:
    pass
try:
    server.close()
except:
    pass

print("YOLO: сокеты закрыты", flush=True)