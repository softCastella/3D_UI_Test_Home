# 버그 리포트: Meta Quest Link 오디오만 출력되고 화면이 표시되지 않음

| 항목 | 내용 |
|---|---|
| 날짜 | 2026-07-05 |
| 상태 | Meta Horizon Link 복구 진행 중, 재검증 필요 |
| 영향 범위 | PC Meta Quest Link 및 Unity Editor OpenXR Play Mode |
| 심각도 | 높음 — XR 화면 출력 및 Unity HMD 테스트 불가 |
| 연결 방식 | 유선 Quest Link |

## 환경

- OS: Windows PC
- GPU: NVIDIA GeForce GTX 1660 Ti (6 GB)
- Meta Horizon Link: 1.115.0
- Meta PC 런타임 로그 버전: 85.0.0.306.552
- Unity: 6000.4.8f1
- URP: 17.4.0
- OpenXR Plugin: 1.16.1
- XR Interaction Toolkit: 3.4.1
- XR Hands: 1.7.3
- 프로젝트: `3D_UI_Test_home`
- 테스트 대상 씬: `Assets/Scenes/Confined Space Scene_half.unity`

## 증상

- Quest Link가 연결된 것처럼 보이거나 오디오가 헤드셋으로 출력된다.
- 헤드셋 화면에는 PC VR 또는 Unity Play Mode 영상이 정상 표시되지 않는다.
- Unity Game View와 HMD 화면이 동기화되지 않는다.
- 장애 발생 후 PC에서 HMD가 감지되지 않는 상태로 전환된다.

## 재현 절차

1. Quest 헤드셋을 USB 케이블로 PC에 연결한다.
2. Meta Quest Link를 시작한다.
3. Unity 프로젝트를 Windows/Standalone 대상으로 열고 Play Mode를 실행한다.
4. 헤드셋의 오디오와 영상 출력을 확인한다.
5. 일정 시간 후 Link 화면이 중단되거나 영상이 표시되지 않는 상태를 확인한다.

## 기대 결과

- Meta Quest Link 홈 화면이 헤드셋에 계속 표시된다.
- Unity Play Mode 진입 시 OpenXR 디스플레이 세션이 시작된다.
- Unity Game View와 헤드셋 영상이 동기화되고 헤드 트래킹이 정상 동작한다.

## 실제 결과

- 오디오는 출력되지만 영상이 표시되지 않거나 갱신되지 않는다.
- Meta 런타임이 HMD를 `NotDetected` 상태로 보고한다.
- Link 영상 전송 구성요소가 초기화되지 않는다.
- Unity의 Standalone XR 자동 초기화도 꺼져 있어 Meta Link가 정상이어도 Play Mode에서 HMD 세션이 시작되지 않는 별도 설정 문제가 있었다.

## 조사 결과

### Meta Quest Link 런타임

- Meta OpenXR 런타임 등록은 정상이다.
  - `C:\Program Files\Meta Horizon\Support\oculus-runtime\oculus_openxr_64.json`
- `OVRService`는 자동 시작 및 실행 상태였다.
- 2026-07-05 10:56~10:58 사이에는 Link 영상 스트림이 실제로 시작됐다.
  - `Streaming: video stream started`
  - 헤드셋에서 5,800프레임 이상 디코딩됨
- 이후 `OculusDash`와 `OVRServer` 충돌 덤프가 생성됐다.
- 재시작 후에도 다음 오류가 반복됐다.

```text
Code: -6100 -- ovrError_XRStreamingGeneralIssue
XrsTransportApiClient: Failed to initialize transport api client.
Link over DISCO: DiscoHighwindDapHostRipcClient InitClient() failed!
Server (highwind_transport_api_server) not found.
Server (highwind_dap_server) not found.
TypesafeLogger: Error INITIALIZATION_FAILED
```

- HMD 상태:

```text
State: NotDetected
IsDetected: 0
IsReady: 0
Mode: Unknown
```

- Windows의 현재 PnP 장치 목록에서도 Quest/Oculus USB 장치가 확인되지 않았다.
- Meta Remote Desktop 구성요소가 연속 11회 충돌한 기록도 확인됐다.

### Unity 프로젝트 설정

- `Assets/XR/XRGeneralSettingsPerBuildTarget.asset`에서 Standalone 설정의 XR 자동 초기화가 꺼져 있었다.

```yaml
m_Name: Standalone Settings
m_InitManagerOnStart: 0
```

- 해당 값을 `1`로 복구했다.
- Standalone과 Android 모두 OpenXR Loader가 등록되어 있다.
- Windows의 활성 OpenXR 런타임은 Meta 런타임으로 올바르게 설정되어 있다.

## 원인 판단

두 문제가 동시에 존재했다.

1. **주 원인:** Meta Quest Link 런타임의 영상 전송 프로세스가 충돌 또는 초기화 실패하여 HMD 영상 세션이 종료됨.
2. **별도 Unity 설정 문제:** Standalone의 `Initialize XR on Startup`이 꺼져 있어 Unity Play Mode가 OpenXR HMD 세션을 자동 시작하지 않음.

오디오 경로는 영상 전송 경로와 별도로 유지될 수 있어, 런타임 영상 장애 중에도 오디오만 출력된 것으로 판단된다.

## 수행한 조치

- Meta OpenXR 활성 런타임 확인: 정상
- `OVRService` 상태 확인: 실행 중
- Meta/Oculus 관련 사용자 프로세스 종료
- `OVRServer_x64` 재생성 확인: 새 PID로 시작됨
- 재시작 후 오류 재현 확인
- Unity Standalone XR 자동 초기화 복구
- `C:\Program Files\Meta Horizon\Setup.exe /repair` 실행

## 후속 검증

- [ ] Meta Horizon Link Repair 완료
- [ ] PC 재부팅
- [ ] Quest 헤드셋 완전 재부팅
- [ ] USB 케이블 재연결 후 Windows PnP 장치 인식 확인
- [ ] Meta Quest Link 홈 화면 영상 출력 확인
- [ ] 새 Meta 서비스 로그에서 오류 `-6100` 재발 여부 확인
- [ ] Unity Build Profile이 Windows/Standalone인지 확인
- [ ] Unity Play Mode에서 OpenXR 디스플레이 초기화 확인
- [ ] Game View와 HMD 화면 동기화 및 헤드 트래킹 확인

## 관련 로그

- `%LOCALAPPDATA%\Oculus\Service_2026-07-05_10.54.12.txt`
- `%LOCALAPPDATA%\Oculus\LinkClient_2026-07-05_10.54.12.txt`
- `%LOCALAPPDATA%\Oculus\Service_2026-07-05_11.02.51.txt`
- `%LOCALAPPDATA%\Oculus\OVRServerBreakpad\`
- `%LOCALAPPDATA%\Unity\Editor\Editor.log`

> 외부 제출 시 로그에 포함될 수 있는 사용자 ID, 장치 ID, 머신 ID 등 개인정보를 제거해야 한다.
