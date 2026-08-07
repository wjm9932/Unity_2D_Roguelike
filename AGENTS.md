# AGENTS.md

## Project Description
- 2D 로그라이크 게임

## Develop Environment

- Unity 6 프로젝트
- C# 사용
- 유니티 코루틴 대신 UniTask 사용

## Coding conventions

- 필드는 camelCase
- 프로퍼티와 메서드는 PascalCase
- 불필요한 LINQ 사용 금지
- 기존 프로젝트의 네임스페이스 구조를 유지한다

## Validation

- 수정 후 Unity 컴파일 오류를 확인한다
- 가능하면 관련 테스트를 실행한다
- .meta 파일을 임의로 삭제하지 않는다