# 버그 리포트: PPE Room 높이·조명·환경 반사

| 항목 | 내용 |
|---|---|
| 날짜 | 2026-07-03 |
| 대상 씬 | `Assets/Scenes/3_PPE_Room.unity` |
| 환경 | Unity 6000.4.8f1 / URP 17.4.0 / OpenXR / Meta Quest·PC VR |
| 관련 코드 | `Assets/Scripts/PPEBackgroundRoom.cs` |
| 상태 | 수정 진행, HMD 실기 검증 필요 |

## 요약

PPE Room의 높이와 위치를 조정하는 과정에서 방 루트 Transform을 Y축으로 확대하면 문과 덕트까지 늘어나는 문제가 확인됐다. 방 생성 파라미터를 사용하는 방식으로 수정했다. 이후 Unity Scene 뷰에서는 밝아 보이는 천장 조명이 VR에서 잘 보이지 않았고, Lit 재질 적용 시 파란 환경광과 반사가 방 전체 및 FBX에 강하게 나타났다. 조명 패널, Bloom, 재질 범위, 반사 설정을 단계적으로 조정했으며 씬 전체 환경광 변경은 물체가 검게 보여 롤백했다.

## BUG-001: 방 높이 변경 시 문과 덕트가 함께 늘어남

### 증상

- 방 루트의 Y Scale을 `2`로 변경하면 문과 덕트도 세로로 늘어났다.
- 플레이 모드 종료 시 외부에서 수정한 루트 Scale이 Unity에 의해 다시 저장되기도 했다.

### 원인

- 문과 덕트가 `PPE Background Room`의 자식이어서 부모 Scale을 상속했다.
- 방 구조는 `PPEBackgroundRoom.roomHeight`를 기준으로 절차적으로 생성되므로 루트 Scale 변경이 적절하지 않았다.

### 조치

- 방 루트 Scale을 `(1, 1, 1)`로 복원했다.
- `roomHeight`를 `7.5`에서 `11.25`로 변경해 원래 높이의 1.5배로 만들었다.
- 방 위치는 `(0, -2.79, 11.05)`로 유지했다.
- 문과 덕트는 원래 크기를 유지했다.

## BUG-002: Scene 뷰에서는 밝은 조명이 VR에서 잘 보이지 않음

### 증상

- Unity Scene 뷰에서는 천장 등이 밝게 보였지만 VR 카메라에서는 발광감이 거의 없었다.

### 원인

- VR 카메라의 Post Processing이 비활성 상태였다.
- 방 표면 재질이 URP Unlit이어서 Spot Light의 영향을 받지 않았다.
- 천장 등은 발광 재질이 아니라 텍스처에 그려진 밝은 영역이었다.
- 기본 Bloom 프로필은 존재했지만 강도가 `0`이었다.

### 조치

- PPE Room 활성화 시 VR 카메라의 Post Processing을 코드에서 활성화한다.
- PPE Room 전용 저강도 Bloom Volume을 런타임 생성한다.
- 두 천장 광원에 Lit+Emission 발광 패널을 추가했다.
- Quest 성능을 고려해 조명 그림자와 고품질 Bloom은 사용하지 않는다.

## BUG-003: 방 전체를 Lit로 변경하면 파란색으로 물듦

### 증상

- 벽·바닥·천장을 Lit로 변경하자 방 전체가 파랗게 보였다.

### 원인

- Lit 재질이 씬의 Skybox 환경광과 반사를 받았다.

### 조치

- 벽과 바닥은 URP Unlit으로 복원했다.
- 천장만 URP Lit을 사용한다.
- 천장에는 Bloom 임계값 이하의 약한 백색 Emission을 추가해 암회색을 완화했다.

## BUG-004: 덕트에 파란 환경 반사가 강하게 나타남

### 증상

- 덕트 표면이 파란색으로 반사되어 보였다.

### 원인

- `PPE_Duct_LightMetal.mat`의 환경 반사, Metallic, Smoothness, Specular가 활성화되어 있었다.

### 조치

- Environment Reflections를 비활성화했다.
- Metallic을 `0.08`에서 `0.02`로 낮췄다.
- Smoothness를 `0.28`에서 `0.12`로 낮췄다.
- Specular Highlights를 비활성화했다.

## BUG-005: 발광 패널과 천장 사이의 등 테두리가 흐려짐

### 증상

- 천장을 밝게 만든 뒤 등 테두리까지 회색으로 보여 발광 패널의 경계가 잘 보이지 않았다.

### 조치

- 발광 패널 뒤에 짙은 무광 Unlit 프레임을 추가했다.
- 프레임은 현재 패널 위치와 크기를 기준으로 자동 갱신된다.

## BUG-006: 플레이 모드 진입 시 광원 위치와 패널 크기가 초기화됨

### 증상

- 편집 모드에서 변경한 광원 위치와 패널 크기가 플레이 모드 실행 후 기본값으로 돌아갔다.

### 원인

- 광원이 `Generated Image Room` 아래에서 `DontSaveInEditor` 상태로 생성됐고 `BuildRoom()` 실행 때 삭제·재생성됐다.

### 조치

- 기존 광원을 자동 생성 루트에서 방 루트로 이동해 일반 씬 오브젝트로 보존한다.
- 동일한 이름의 광원이 있으면 재생성하지 않고 기존 Transform, 패널 크기, Light 설정을 재사용한다.

## BUG-007: 씬 전체 환경광 중성화 시 물체가 검게 보임

### 시도

- Ambient Mode를 중성 회색 Flat으로 변경했다.
- Reflection Intensity를 `1`에서 `0.3`으로 낮췄다.

### 결과

- 일부 FBX가 지나치게 어둡거나 검게 보였다.

### 조치

- Ambient Mode, Ambient Sky Color, Reflection Intensity를 기존 Skybox 설정으로 롤백했다.
- FBX별 재질 조정 방식으로 유지한다.

## 천장 형상 보정

- 문 반대편에서 천장 흰색 면이 몰딩을 덮는 문제를 줄이기 위해 뒤쪽 천장 길이를 `0.35m` 줄였다.
- 천장 중심을 문 쪽으로 `0.175m` 이동했다.
- 이 조정은 환경광 롤백 이후에도 유지한다.

## 검증 필요 항목

- [ ] Quest 실기에서 발광 패널과 Bloom의 밝기 및 비용 확인
- [ ] 편집한 광원 위치와 패널 크기가 씬 저장 후 플레이 모드에서도 유지되는지 확인
- [ ] 천장 뒤쪽 몰딩이 양쪽 시야에서 자연스럽게 노출되는지 확인
- [ ] 덕트가 지나치게 무광이거나 어둡지 않은지 확인
- [ ] 천장 백색 Emission이 텍스처 디테일을 과도하게 씻어내지 않는지 확인

## 관련 파일

- `Assets/Scenes/3_PPE_Room.unity`
- `Assets/Scripts/PPEBackgroundRoom.cs`
- `Assets/UIs/Facilities/PPE_Room/Duct/PPE_Duct_LightMetal.mat`

