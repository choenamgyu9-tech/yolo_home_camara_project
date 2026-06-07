# -*- coding: utf-8 -*-

import argparse
import json
import os
from pathlib import Path
import cv2
from ultralytics import YOLO, YOLOE


def seconds_to_time_str(seconds: float) -> str:
    total_seconds = int(seconds)
    h = total_seconds // 3600
    m = (total_seconds % 3600) // 60
    s = total_seconds % 60
    return f"{h:02d}:{m:02d}:{s:02d}"


def load_prompt_model(model_path: Path, keywords: list[str]):
    model_name = model_path.name.lower()

    if "world" in model_name:
        model = YOLO(str(model_path))
        model.set_classes(keywords)
        return model

    model = YOLOE(str(model_path))
    text_embeddings = model.get_text_pe(keywords)
    model.set_classes(keywords, text_embeddings)
    return model


def main():
    parser = argparse.ArgumentParser()

    parser.add_argument("--video", required=True, help="분석할 영상 파일 경로")
    parser.add_argument("--keywords", required=True, help="쉼표로 구분된 키워드 목록")
    parser.add_argument("--model", default="models/yoloe-11s-seg.pt", help="YOLOE 모델 경로")
    parser.add_argument("--output", default="output/detection_result.json", help="결과 JSON 경로")
    parser.add_argument("--snapshot-dir", default="../Assets/Snapshots", help="스냅샷 저장 폴더")
    parser.add_argument("--conf", type=float, default=0.5, help="신뢰도 기준값")
    parser.add_argument("--vid-stride", type=int, default=5, help="프레임 분석 간격")
    parser.add_argument("--imgsz", type=int, default=480, help="입력 이미지 크기")

    args = parser.parse_args()

    video_path = Path(args.video)
    keywords = [k.strip() for k in args.keywords.split(",") if k.strip()]

    if not video_path.exists():
        raise FileNotFoundError(f"영상 파일을 찾을 수 없습니다: {video_path}")

    base_dir = Path(__file__).resolve().parent
    model_path = Path(args.model)
    if not model_path.is_absolute():
        model_path = base_dir / model_path

    output_path = Path(args.output)
    if not output_path.is_absolute():
        output_path = base_dir / output_path

    snapshot_dir = Path(args.snapshot_dir)
    if not snapshot_dir.is_absolute():
        snapshot_dir = base_dir / snapshot_dir

    output_path.parent.mkdir(parents=True, exist_ok=True)
    snapshot_dir.mkdir(parents=True, exist_ok=True)

    # 영상 FPS 확인
    cap = cv2.VideoCapture(str(video_path))
    fps = cap.get(cv2.CAP_PROP_FPS)
    frame_count = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
    if fps <= 0:
        fps = 30
    cap.release()
    total_result_count = max(1, (frame_count + args.vid_stride - 1) // args.vid_stride)

    # YOLOE 모델 로드
    model = load_prompt_model(model_path, keywords)

    results = model.predict(
        source=str(video_path),
        conf=args.conf,
        save=False,
        stream=True,
        vid_stride=args.vid_stride,
        imgsz=args.imgsz
    )

    detection_events = []

    for result_index, result in enumerate(results):
        # result_index는 분석된 프레임 순서
        original_frame_index = result_index * args.vid_stride
        progress = min(99, int(((result_index + 1) / total_result_count) * 100))
        print(f"PROGRESS:{progress}", flush=True)
        event_seconds = original_frame_index / fps
        event_time = seconds_to_time_str(event_seconds)

        if result.boxes is None or len(result.boxes) == 0:
            continue

        frame = result.orig_img

        for box_index, box in enumerate(result.boxes):
            cls_id = int(box.cls[0])
            confidence = float(box.conf[0])
            keyword = result.names[cls_id]
            xyxy = box.xyxy[0].tolist()

            # conf는 predict 단계에서 걸러지지만, 안전하게 한 번 더 필터링
            if confidence < args.conf:
                continue

            x1, y1, x2, y2 = map(int, xyxy)

            snapshot_name = f"{keyword}_{original_frame_index}_{box_index}.jpg"
            snapshot_path = snapshot_dir / snapshot_name

            # 원본 프레임에 박스 그리기
            frame_copy = frame.copy()
            cv2.rectangle(frame_copy, (x1, y1), (x2, y2), (0, 255, 0), 2)
            cv2.putText(
                frame_copy,
                f"{keyword} {confidence:.2f}",
                (x1, max(y1 - 10, 20)),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.7,
                (0, 255, 0),
                2
            )
            cv2.imwrite(str(snapshot_path), frame_copy)

            detection_events.append({
                "videoPath": str(video_path),
                "eventTime": event_time,
                "eventSeconds": round(event_seconds, 2),
                "frameIndex": original_frame_index,
                "keyword": keyword,
                "confidence": round(confidence, 4),
                "snapshotPath": str(snapshot_path),
                "box": {
                    "x1": x1,
                    "y1": y1,
                    "x2": x2,
                    "y2": y2
                }
            })

    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(detection_events, f, ensure_ascii=False, indent=2)

    print("PROGRESS:100", flush=True)

    print(f"분석 완료: {len(detection_events)}개 이벤트")
    print(f"결과 저장 경로: {output_path}")


if __name__ == "__main__":
    main()
