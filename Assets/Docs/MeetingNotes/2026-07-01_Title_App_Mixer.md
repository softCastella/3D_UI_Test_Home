# 2026-07-01 타이틀·앱 시작 흐름·혼합기 내부 환경 회의록

- 날짜: 2026-07-01
- 프로젝트: 3D_UI_Test_home
- Unity: 6000.4.8f1
- 대상 플랫폼: Meta Quest/Android 및 PC OpenXR

## 오늘의 목표

- VR 애플리케이션 시작 씬과 타이틀 씬의 흐름을 확정한다.
- 타이틀 로고와 파트너 로고의 표시 방식 및 배치를 확정한다.
- 화학 혼합기 내부를 VR에서 360도로 확인할 수 있는 공간으로 구현한다.

## 합의 및 결정 사항

### 애플리케이션 시작 흐름

- 애플리케이션 최초 실행 씬은 `Assets/Scenes/App.unity`로 한다.
- Build Settings의 씬 순서는 다음과 같이 구성한다.
  1. `App`
  2. `TitleScene`
  3. `Confined Space Scene_half`
- `AppSceneBootstrap`이 App 씬 시작 후 한 프레임을 기다리고 `TitleScene`을 비동기로 로드한다.
- App 씬의 Main Camera 배경은 검정색 Solid Color로 사용한다.
- 타이틀 연출이 끝난 뒤 다음 메인 씬으로 자동 전환하는 기능은 현재 적용하지 않는다.

### 타이틀 씬 로고 배치

- `Canvas/Logos`의 파트너 로고는 화면 하단 중앙에 배치한다.
- 로고 순서는 왼쪽부터 `to21 → hongdae → team`으로 한다.
- 자동 정렬에는 `Horizontal Layout Group`을 사용한다.
- 각 로고는 원본 비율을 유지하고 동일한 시각 높이로 정렬한다.

### 타이틀 로고 연출

- 타이틀 로고가 먼저 페이드 인되고, 설정된 지연 후 파트너 로고가 페이드 인된다.
- 두 로고 그룹 모두 페이드 아웃하지 않고 화면에 유지한다.
- 페이드 시간과 로고 사이 지연은 `TitleSplashController` Inspector에서 조정 가능하게 한다.
- XR 시작 직후 타이틀 로고가 번쩍이는 현상을 방지하기 위해 다음 방식을 사용한다.
  - 알파 0 상태에서 Canvas 강제 갱신
  - 기본 3프레임 GPU/Canvas 예열
  - 타이틀 로고의 `CanvasRenderer.Cull Transparent Mesh` 비활성화
  - 예열 이후 실제 페이드 시작

### 화학 혼합기 내부 환경

- 일반 큐브맵 스카이박스 대신 실제 깊이와 머리 위치 이동이 반영되는 3D 내부 공간을 사용한다.
- 기본 구조는 다음과 같다.
  - 원통형 스테인리스 내벽
  - 원형으로 성형된 상단 돔
  - 오목한 하단
  - 중앙 회전축
  - 다단 혼합 블레이드
  - 원형 보강링
- 내부 벽의 면 방향은 안쪽을 향하게 하며, 외부에서는 탱크 벽이 렌더링되지 않도록 한다.
- 전용 URP 셰이더로 브러시드 스테인리스, 패널 이음선과 금속 하이라이트를 표현한다.
- 생성 도구 메뉴는 `Tools > Environment > Create Chemical Mixer Interior`를 사용한다.
- 기존 혼합기를 카메라 중심으로 옮길 때는 `Tools > Environment > Center Selected Mixer On Main Camera`를 사용한다.
- VR 카메라는 혼합기 중앙에 놓고, Root Scale은 `(1, 1, 1)`, 반경은 기본 3~5m 범위를 권장한다.
- 현재 확인용 씬은 `Assets/Scenes/insideMixer.unity`이다.

## 구현 파일

- `Assets/Scenes/App.unity`
- `Assets/Scenes/TitleScene.unity`
- `Assets/Scenes/insideMixer.unity`
- `Assets/Scripts/AppSceneBootstrap.cs`
- `Assets/Scripts/TitleSplashController.cs`
- `Assets/Scripts/ChemicalMixerInterior.cs`
- `Assets/Editor/ChemicalMixerInteriorMaker.cs`
- `Assets/Shaders/ChemicalMixerInterior.shader`
- `Docs/Bug/2026-07-01_TitleScene-XR-Logo-Fade-Flash.md`

## 검증 결과

- 타이틀 로고의 초기 번쩍임이 사전 렌더링 방식으로 해결됨을 확인했다.
- 파트너 로고가 하단 중앙에서 지정 순서로 자연스럽게 페이드 인됨을 확인했다.
- 혼합기 내부에서 벽, 상단 돔, 축과 블레이드가 보이는 것을 확인했다.
- Main Camera 중심 배치를 통해 VR에서 고개를 돌렸을 때 전 방향을 볼 수 있는 구조로 구성했다.

## 후속 확인 사항

- 실제 Quest 기기에서 혼합기 내부 360도 시야와 근거리 클리핑을 확인한다.
- 금속 셰이더의 반사 강도와 밝기가 HMD에서 과도하지 않은지 조정한다.
- App 씬에서 TitleScene으로의 전환 시 검정 프레임과 XR 초기화 타이밍을 확인한다.
- 타이틀 이후 메인 씬으로 넘어가는 최종 조건과 시점은 추후 결정한다.
