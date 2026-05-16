\# YOLO Home Camera Project



WPF 기반 홈캠 이벤트 관리 프로그램 프로젝트입니다.



\## 1. 프로젝트 처음 내려받기(Clone)



처음 프로젝트를 받는 사람은 원하는 폴더에서 아래 명령어를 실행합니다.



```bash

git clone https://github.com/choenamgyu9-tech/yolo\_home\_camara\_project.git



이후 생성된 폴더로 이동합니다.



cd yolo\_home\_camara\_project



=================================================================

2\. 작업 전 최신 코드 받기(Pull)



작업을 시작하기 전에는 항상 최신 코드를 먼저 받아옵니다.



git pull origin main



또는 이미 main 브랜치가 origin/main을 추적 중이라면 아래처럼 입력해도 됩니다.



git pull



=================================================================

3\. 작업 상태 확인



현재 변경된 파일을 확인합니다.



git status



=================================================================

4\. 변경 내용 추가(Add)



수정한 모든 파일을 Git에 추가합니다.



git add .



특정 파일만 추가하고 싶다면 파일명을 지정합니다.



git add 파일명



예시:



git add yolo\_home\_camera\_project/MainWindow.xaml



=================================================================

5\. 커밋하기(Commit)



변경 내용을 하나의 기록으로 저장합니다.



git commit -m "작업 내용 설명"



예시:



git commit -m "메인 화면 레이아웃 추가"



커밋 메시지는 어떤 작업을 했는지 알아볼 수 있게 작성합니다.



=================================================================

6\. GitHub에 올리기(Push)



로컬에서 커밋한 내용을 GitHub 저장소에 올립니다.



git push origin main



또는 추적 브랜치가 설정되어 있다면 아래처럼 입력해도 됩니다.



git push



=================================================================

7\. 일반적인 작업 순서



작업할 때는 보통 아래 순서로 진행합니다.



git pull origin main

git status

git add .

git commit -m "작업 내용 설명"

git push origin main



예시:



git pull origin main

git add .

git commit -m "이벤트 로그 화면 추가"

git push origin main



=================================================================

8\. 주의사항



아래 파일과 폴더는 GitHub에 올리지 않습니다.



.vs/

bin/

obj/

Debug/

Release/

개인 API 키

비밀번호

인증 파일

개인 설정 파일



이 프로젝트에서는 .gitignore를 사용해 자동 생성 파일이 GitHub에 올라가지 않도록 관리합니다.



=================================================================

9\. 충돌이 발생한 경우



여러 사람이 같은 파일을 수정하면 충돌이 발생할 수 있습니다.



충돌이 발생하면 바로 push하지 말고, 충돌 표시가 난 파일을 열어 어떤 내용을 남길지 정리한 뒤 다시 커밋합니다.



충돌 표시 예시:



<<<<<<< HEAD

내가 수정한 내용

=======

다른 사람이 수정한 내용

>>>>>>> origin/main



필요한 내용만 남기고 위 표시들을 삭제한 뒤 다시 저장합니다.



git add .

git commit -m "충돌 해결"

git push origin main



저장한 뒤 PowerShell에서 아래 명령어를 입력해.



```powershell

git add README.md

git commit -m "README에 Git 사용 방법 추가"

git push

