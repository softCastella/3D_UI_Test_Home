# 버그 리포트: Meta Quest Link 영상 중단 및 반복 연결 해제

| 항목 | 내용 |
|---|---|
| 최초 기록 | 2026-07-05 |
| 재발 확인 | 2026-07-16 |
| 상태 | Meta Quest Link 런타임 복구 및 재검증 필요 |
| 영향 범위 | PC Meta Quest Link 및 Unity Editor OpenXR Play Mode |
| 심각도 | 높음 — XR 영상 출력과 Unity HMD 테스트 불가 |
| 확인된 연결 방식 | 유선 Quest Link |

## 환경

- OS: Windows PC
- GPU: NVIDIA GeForce GTX 1660 Ti 6 GB
- Meta Horizon Link: 최초 조사 당시 1.115.0
- Meta PC 소프트웨어 로그 버전: 최초 조사 당시 85.0.0.306.552
- Unity: 6000.4.8f1
- URP: 17.4.0
- OpenXR Plugin: 1.16.1
- XR Interaction Toolkit: 3.4.1
- XR Hands: 1.7.3
- 프로젝트: `3D_UI_Test_home`
- 주요 테스트 씬: `Assets/Scenes/Confined Space Scene_half.unity`

## 증상

- Quest Link가 연결된 것처럼 보이거나 오디오가 헤드셋으로 출력되지만 영상이 표시되지 않는다.
- Link 영상이 정상적으로 시작된 뒤 짧은 간격으로 연결이 반복해서 끊긴다.
- Unity Play Mode 실행 중 HMD 영상과 Game View 동기화가 중단된다.
- 장애 발생 후 Meta 런타임이 HMD를 `NotDetected` 상태로 보고할 수 있다.
- Unity OpenXR가 종료와 재초기화를 반복하거나 최종적으로 Display 초기화에 실패한다.

## 기대 결과

- Meta Quest Link 홈 화면이 헤드셋에 계속 표시된다.
- Unity Play Mode 진입 시 OpenXR 디스플레이 세션이 안정적으로 시작된다.
- Game View와 HMD 영상이 동기화되고 헤드 트래킹이 정상 동작한다.

## 최초 조사 결과 — 2026-07-05

### Meta Quest Link 런타임

- Meta OpenXR 런타임 등록은 정상이었다.
  - `C:\Program Files\Meta Horizon\Support\oculus-runtime\oculus_openxr_64.json`
- `OVRService`는 자동 시작 및 실행 상태였다.
- 10:56~10:58 사이 Link 영상 스트리밍이 실제로 시작됐고 약 5,800프레임 이상 디코딩됐다.
- 이후 `OculusDash`와 `OVRServer` 충돌 덤프가 생성됐다.
- 재시작 후 다음 오류가 반복됐다.

```text
Code: -6100 -- ovrError_XRStreamingGeneralIssue
XrsTransportApiClient: Failed to initialize transport api client.
Link over DISCO: DiscoHighwindDapHostRipcClient InitClient() failed!
Server (highwind_transport_api_server) not found.
Server (highwind_dap_server) not found.
TypesafeLogger: Error INITIALIZATION_FAILED
```

HMD 상태는 다음과 같았다.

```text
State: NotDetected
IsDetected: 0
IsReady: 0
Mode: Unknown
```

- 당시 Windows PnP 장치 목록에서도 Quest/Oculus USB 장치가 확인되지 않았다.
- Meta Remote Desktop 구성 요소의 연속 충돌 기록도 확인됐다.

### Unity 프로젝트 설정

- `Assets/XR/XRGeneralSettingsPerBuildTarget.asset`에서 Standalone의 XR 자동 초기화가 꺼져 있었다.

```yaml
m_Name: Standalone Settings
m_InitManagerOnStart: 0
```

- 해당 값은 이후 `1`로 복구했다.
- Standalone과 Android 모두 OpenXR Loader가 등록되어 있었다.
- Windows 활성 OpenXR 런타임도 Meta 런타임으로 올바르게 설정되어 있었다.

이 설정 문제는 Unity가 Play Mode에서 OpenXR를 자동 시작하지 못하게 할 수 있지만, Meta Link 자체의 영상 전송 프로세스 충돌과는 별개의 문제다.

## 재발 조사 — 2026-07-16

Unity Editor 로그와 Meta 서비스 로그의 시간을 비교한 결과, Unity가 먼저 Quest Link를 종료한 것이 아니었다. Meta 런타임의 IPC 및 영상 전송 계층이 먼저 사라졌고 Unity는 세션 손실을 전달받아 OpenXR 재시작을 수행했다.

확인된 순서는 다음과 같다.

1. 22:53:40경 Meta Runtime IPC 서버 연결이 동시에 해제됐다.
2. 22:53:41 Meta D3D11 compositor가 `ovrError_DisplayLost (-6000)`를 보고했다.
3. Unity 세션이 `XR_SESSION_STATE_LOSS_PENDING`으로 전환됐다.
4. Unity의 `xrWaitFrame`에서 `XR_ERROR_SESSION_LOST`가 발생했다.
5. Unity OpenXR Restarter가 종료와 초기화를 반복했다.
6. Meta 서비스는 `highwind_transport_api_server`와 `highwind_dap_server`를 찾지 못했다.
7. `ovrError_XRStreamingGeneralIssue (-6100)`와 `INITIALIZATION_FAILED`가 계속 반복됐다.
8. `OculusDash`도 비정상 종료했으며 HMD가 `NotDetected`로 전환됐다.

대표 로그:

```text
compEndFrame error: ovrError_DisplayLost
XR_SESSION_STATE_SYNCHRONIZED -> XR_SESSION_STATE_LOSS_PENDING
XrResult failure [XR_ERROR_SESSION_LOST]
Shutting down OpenXR.
Initializing OpenXR.
Failed to initialize subsystem OpenXR Display [error: 1]
```

이 시점에 Unity 스크립트 컴파일, 도메인 리로드, 셰이더 컴파일 또는 프로젝트 예외가 먼저 연결을 종료한 흔적은 확인되지 않았다.

Unity 로그에 함께 기록된 아래 메시지는 Android Player Connection/ADB reverse 관련 별도 문제이며, PC Quest Link 세션 손실의 직접 원인은 아니다.

```text
Connection to Android device failed: Unable to reverse network traffic to device.
```

## 원인 판정

1. **주 원인:** Meta Quest Link의 Highwind/DISCO 영상 전송 또는 IPC 서버가 중단되어 HMD 디스플레이 세션이 손실됨.
2. **가능한 촉발 요인:** USB 장치 순간 탈락, 케이블·포트 불안정, Meta 런타임 구성 요소 손상 또는 Meta 소프트웨어 업데이트 불일치.
3. **Unity의 동작:** Unity OpenXR는 외부 런타임에서 전달된 `SESSION_LOST`를 처리하며 자동 재시작했을 뿐, 이번 로그에서 최초 연결 해제 주체로 확인되지 않음.
4. **별도 Unity 설정 문제:** 과거 Standalone의 `Initialize XR on Startup` 비활성화는 수정됐으며 Link 런타임 장애와 구분해야 함.

## Meta PC 앱 위치

설치 폴더:

```text
C:\Program Files\Meta Horizon\
```

일반적인 클라이언트 실행 파일:

```text
C:\Program Files\Meta Horizon\Support\oculus-client\OculusClient.exe
```

Windows 시작 메뉴에서는 설치 버전에 따라 `Meta Quest Link`, `Meta Horizon` 또는 이전 명칭인 `Oculus`로 표시될 수 있다.

## 복구 절차

1. Unity Play Mode를 종료한다.
2. Quest에서 Quest Link/Air Link를 종료한다.
3. Meta PC 앱을 완전히 종료한다.
4. PC와 Quest 헤드셋을 모두 완전히 재부팅한다.
5. 유선 Link라면 USB 허브와 PC 전면 포트를 피하고 다른 USB 3.x 포트에 직결한다.
6. Windows 장치 관리자에서 Quest/Oculus USB 장치가 실제로 인식되는지 확인한다.
7. Meta Quest Link 설치 프로그램을 다시 실행하여 Repair를 수행한다.
8. Repair 완료 후 PC를 재부팅하고 Meta Quest Link 홈 화면부터 검증한다.
9. Link가 안정된 뒤에만 Unity Play Mode를 시작한다.

### Repair 메뉴 위치

Repair는 Meta PC 앱 내부 설정 메뉴에 있지 않다. Meta 공식 설치 프로그램인 `OculusSetup.exe`를 다시 실행해야 한다.

1. Meta 공식 배포 페이지에서 Meta Quest Link PC 앱 설치 프로그램을 다시 받는다.
2. Meta PC 앱이 종료된 상태에서 `OculusSetup.exe`를 실행한다.
3. 기존 설치가 감지되면 `Repair` 또는 `복구`를 선택한다.
4. 복구가 끝나면 PC를 재부팅한다.

과거 문서에 기록됐던 다음 명령은 설치 버전에 따라 파일이 존재하지 않거나 지원되지 않을 수 있으므로 기본 복구 방법으로 사용하지 않는다.

```text
C:\Program Files\Meta Horizon\Setup.exe /repair
```

## 재검증 체크리스트

- [ ] Meta Quest Link 설치 프로그램 Repair 완료
- [ ] PC 재부팅 완료
- [ ] Quest 헤드셋 완전 재부팅 완료
- [ ] USB 재연결 후 Windows PnP 장치 인식 확인
- [ ] Meta Quest Link 홈 화면 영상이 안정적으로 유지됨
- [ ] Meta 서비스 로그에서 오류 `-6000`, `-6100` 재발 여부 확인
- [ ] `highwind_transport_api_server` 및 `highwind_dap_server` 누락 오류가 더 이상 반복되지 않음
- [ ] Unity Build Profile이 Windows/Standalone인지 확인
- [ ] Unity에서 스크립트·셰이더 컴파일이 끝난 후 Play Mode 진입
- [ ] Unity OpenXR Display 초기화 확인
- [ ] Game View/HMD 영상 동기화 및 헤드 트래킹 확인
- [ ] 양안 Quest/OpenXR 렌더링 확인

## 관련 로그

최초 조사:

- `%LOCALAPPDATA%\Oculus\Service_2026-07-05_10.54.12.txt`
- `%LOCALAPPDATA%\Oculus\LinkClient_2026-07-05_10.54.12.txt`
- `%LOCALAPPDATA%\Oculus\Service_2026-07-05_11.02.51.txt`

2026-07-16 재발 조사:

- `%LOCALAPPDATA%\Oculus\Service_2026-07-16_21.49.14.txt`
- `%LOCALAPPDATA%\Oculus\LinkClient_2026-07-16_21.49.14.txt`
- `%LOCALAPPDATA%\Oculus\Client_2026-07-16_21.49.23.txt`
- `%LOCALAPPDATA%\Unity\Editor\Editor.log`
- `%LOCALAPPDATA%\Oculus\OVRServerBreakpad\`

> 외부 공유 전에 로그에 포함될 수 있는 사용자 ID, 장치 ID, 머신 ID 등 개인정보를 제거해야 한다.
