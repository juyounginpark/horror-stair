# ShortHorror 협업 가이드

이 문서는 ShortHorror 프로젝트의 이슈, 브랜치, 커밋, Pull Request 협업 규칙을 정의합니다.

기본 작업 흐름은 다음과 같습니다.

`이슈 생성 → 브랜치 생성 → 작업 및 커밋 → PR 생성 → 리뷰 → Squash Merge → 브랜치 삭제`

## 1. 이슈 컨벤션

모든 작업은 GitHub Issue를 먼저 만든 후 시작합니다. 하나의 이슈에는 하나의 명확한 목표만 작성합니다.

### 이슈 제목

```text
[타입] 작업 요약
```

예시:

```text
[Feature] Stage 1 스태미나 시스템 추가
[Fix] 계단에서 플레이어가 미끄러지는 문제 수정
[Refactor] 아이템 상호작용 로직 정리
```

### 이슈 타입과 라벨

| 타입 | 라벨 | 용도 |
| --- | --- | --- |
| Feature | `feature` | 새로운 기능 추가 |
| Fix | `bug` | 버그 수정 |
| Refactor | `refactor` | 기능 변화 없는 코드 개선 |
| Docs | `docs` | 문서 작성 및 수정 |
| Chore | `chore` | 설정, 에셋 정리, 기타 작업 |

### 이슈 작성 규칙

- Stage 번호 또는 적용할 씬을 명시합니다.
- 작업할 내용을 체크박스로 최대한 세분화합니다.
- 참고 이미지, 영상, 문서가 있다면 첨부합니다.
- 완료 조건이 분명하도록 작성합니다.
- 작업 중 범위가 크게 늘어나면 별도 이슈로 분리합니다.

```markdown
## 작업할 내용

- [ ] Shift 입력 처리
- [ ] 스태미나 소모 및 회복
- [ ] 스태미나 UI 연동
- [ ] 플레이 모드 테스트
```

Feature 이슈는 [Feature Issue Form](.github/ISSUE_TEMPLATE/feature.yml)을 사용합니다.

## 2. 이슈 단위 브랜치

브랜치는 반드시 하나의 이슈 단위로 생성합니다. 현재 저장소의 기준 브랜치는 `main`입니다.

### 브랜치 이름

```text
<타입>/<이슈번호>-<짧은-영문-설명>
```

| 작업 | 브랜치 예시 |
| --- | --- |
| 기능 | `feature/12-stamina` |
| 버그 | `fix/18-stair-slide` |
| 리팩터링 | `refactor/24-item-system` |
| 문서 | `docs/30-workflow` |
| 기타 | `chore/35-project-settings` |

브랜치 이름은 소문자 영문과 숫자, 하이픈을 사용합니다.

### 브랜치 생성

```bash
git switch main
git pull origin main
git switch -c feature/12-stamina
```

작업 브랜치를 원격 저장소에 처음 올릴 때:

```bash
git push -u origin feature/12-stamina
```

한 브랜치에서 관련 없는 여러 이슈를 함께 처리하지 않습니다.

## 3. 커밋 컨벤션

커밋 메시지는 Conventional Commits 형식을 사용합니다.

```text
<type>: <변경 내용> (#이슈번호)
```

### 커밋 타입

| 타입 | 용도 |
| --- | --- |
| `feat` | 새로운 기능 |
| `fix` | 버그 수정 |
| `refactor` | 코드 구조 개선 |
| `docs` | 문서 변경 |
| `style` | 동작 변화 없는 포맷 변경 |
| `test` | 테스트 추가 및 수정 |
| `perf` | 성능 개선 |
| `build` | 빌드 및 패키지 설정 |
| `ci` | GitHub Actions 등 CI 설정 |
| `chore` | 에셋 정리 및 기타 작업 |

### 커밋 예시

```text
feat: 스태미나 달리기 기능 추가 (#12)
fix: 벽 접촉 중 점프가 멈추는 문제 수정 (#18)
refactor: 아이템 감지 로직을 3D 오브젝트용으로 변경 (#24)
docs: PR 협업 규칙 추가 (#30)
```

### 커밋 작성 규칙

- 제목은 변경 결과가 드러나도록 간결하게 작성합니다.
- 가능하면 한 커밋에는 하나의 논리적 변경만 포함합니다.
- 정상 동작하지 않는 중간 상태는 공유 브랜치에 올리지 않습니다.
- 자동 생성 파일과 무관한 변경을 함께 커밋하지 않습니다.
- `Library`, `Temp`, `Logs`, `UserSettings` 등 로컬 생성물은 커밋하지 않습니다.

## 4. Pull Request 협업 방식

### PR 제목

```text
[타입] #이슈번호 작업 요약
```

예시:

```text
[Feature] #12 스태미나 시스템 추가
[Fix] #18 계단 미끄러짐 수정
```

### PR 본문

```markdown
## 관련 이슈

Closes #12

## 작업 내용

- Shift 달리기 구현
- 스태미나 소모 및 회복 구현
- Slider UI와 비네트 효과 연결

## 테스트

- [x] Unity Console 오류 없음
- [x] 플레이 모드에서 기능 확인
- [x] 스태미나 0 및 20% 경계값 확인

## 참고 자료

- 스크린샷 또는 영상
```

`Closes #이슈번호`를 작성하면 PR이 머지될 때 연결된 이슈가 자동으로 닫힙니다.

### PR 생성 전 체크리스트

- 최신 `main` 기준으로 충돌 여부를 확인합니다.
- Unity Console의 컴파일 오류와 경고를 확인합니다.
- 변경한 기능을 플레이 모드에서 직접 테스트합니다.
- 불필요한 파일과 개인 설정이 포함되지 않았는지 확인합니다.
- Scene, Prefab, ProjectSettings 변경 사항을 PR 본문에 명시합니다.
- UI 또는 시각적 변경은 스크린샷이나 영상을 첨부합니다.

### 리뷰와 머지

1. 작업을 시작하면 Draft PR을 미리 열어 진행 상황을 공유할 수 있습니다.
2. 작업이 끝나면 자체 리뷰 후 Ready for review로 변경합니다.
3. 리뷰어는 기능, 코드, 테스트, Unity 에셋 충돌 여부를 확인합니다.
4. 수정 요청은 새 커밋으로 반영하고 해결 여부를 답글로 남깁니다.
5. 승인 후 `Squash and merge`를 사용합니다.
6. 머지된 작업 브랜치는 원격과 로컬에서 삭제합니다.

Squash 커밋 메시지 예시:

```text
feat: 스태미나 시스템 추가 (#12)
```

## 5. 최신 main 반영과 충돌 처리

PR 전에 원격 `main`의 최신 변경을 작업 브랜치에 반영합니다.

```bash
git fetch origin
git rebase origin/main
```

Rebase 후 본인만 사용하는 브랜치를 갱신할 때는 다음 명령을 사용합니다.

```bash
git push --force-with-lease
```

공동으로 사용하는 브랜치에는 강제 푸시하지 않습니다. 충돌을 해결한 뒤 Unity에서 Scene과 Prefab이 정상적으로 열리는지 반드시 확인합니다.

## 6. Unity 협업 주의사항

- 같은 Scene이나 Prefab을 동시에 수정하기 전에 작업자를 정합니다.
- `.meta` 파일은 에셋과 함께 반드시 커밋합니다.
- 에셋을 파일 탐색기에서 임의로 이동하지 말고 가능하면 Unity Project 창에서 이동합니다.
- Scene과 Prefab의 대규모 변경은 별도 이슈와 PR로 분리합니다.
- 패키지를 추가했다면 `Packages/manifest.json`과 `Packages/packages-lock.json`을 함께 확인합니다.
- 머지 충돌이 발생한 Scene이나 Prefab을 내용 확인 없이 한쪽 버전으로 덮어쓰지 않습니다.
- PR 머지 후 `main`에서 Unity 프로젝트를 열어 컴파일과 플레이 모드를 최종 확인합니다.

## 7. 작업 완료 기준

다음 조건을 모두 만족하면 이슈를 완료한 것으로 봅니다.

- 이슈의 체크리스트가 모두 완료되었습니다.
- 요구한 기능이 플레이 모드에서 정상 동작합니다.
- Unity Console에 관련 컴파일 오류가 없습니다.
- 필요한 Scene, Prefab, Script 연결이 저장되었습니다.
- PR 리뷰가 완료되고 `main`에 머지되었습니다.
- PR과 연결된 이슈가 닫혔습니다.
