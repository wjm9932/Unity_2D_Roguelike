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
- 기존 프로젝트의 네임스페이스 구조를 유지한다

## Commit Conventions

- 커밋 메세지는 핵심만 Description은 무슨 작업을 수행 했는지 리스트 업

## Validation

- 수정 후 Unity 컴파일 오류를 확인한다
- 가능하면 관련 테스트를 실행한다
- .meta 파일을 임의로 삭제하지 않는다

## Git workflow

- 작업을 완료하고 검증한 뒤 로컬 커밋한다.
- 사용자가 명시적으로 요청하지 않으면 commit amend, rebase, reset, clean을 실행하지 않는다.
- force push는 실행하지 않는다.
- 모든 push는 사용자의 명시적인 승인을 받은 뒤 실행한다.