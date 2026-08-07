# AGENTS.md

## Project Description

- 2D 로그라이크 게임

## Develop Environment

- Unity 6 프로젝트
- C# 사용

## Coding Style / Rules

- 유니티 코루틴 대신 UniTask 사용
- Unity 컴포넌트로 동작해야 하는 경우가 아니라면 MonoBehaviour 상속을 지양하고, 순수 C# 클래스로 구현한다.

## Coding conventions

- 필드는 camelCase
- 프로퍼티와 메서드는 PascalCase
- 불필요한 LINQ 사용 금지

## Commit Conventions

- 커밋 메세지는 핵심만 Description은 무슨 작업을 수행 했는지 리스트 업

## Validation

- .meta 파일을 임의로 삭제하지 않는다
- 수정 후 Unity 컴파일 오류를 확인한다
- Unity 프로젝트 변경 후 Unity 컴파일 검증을 수행한다.
- Unity 실행 또는 컴파일 검증에 실패한 경우, 검증되지 않은 상태로 자동 커밋하지 않는다.
- 검증 실패 원인을 사용자에게 보고하고 변경 사항은 작업 트리에 남겨둔다.

## Unity Validation

- Unity Editor를 이용한 패키지 resolve 및 컴파일 검증은 sandbox 내부에서 실행하지 않는다.
- Unity.exe는 Licensing Client IPC 호환성을 위해 일반 사용자 세션에서 실행한다.

## Git workflow

- 작업을 완료하고 검증한 뒤 로컬 커밋한다.
- 관련 없는 작업은 스테이징 및 커밋하지 않는다.
- 사용자가 명시적으로 요청하지 않으면 commit amend, rebase, reset, clean을 실행하지 않는다.
- force push는 실행하지 않는다.
- 모든 push는 사용자의 명시적인 승인을 받은 뒤 실행한다.