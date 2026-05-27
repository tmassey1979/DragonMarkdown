# DragonMarkdown Wiki Source

This folder contains source pages for the GitHub wiki.

GitHub wikis are stored in a separate repository:

```text
https://github.com/tmassey1979/DragonMarkdown.wiki.git
```

To publish these pages:

```powershell
git clone https://github.com/tmassey1979/DragonMarkdown.wiki.git .tmp\DragonMarkdown.wiki
Copy-Item .\docs\wiki\*.md .\.tmp\DragonMarkdown.wiki\ -Force
Set-Location .\.tmp\DragonMarkdown.wiki
git add -- *.md
git commit -m "docs: update DragonMarkdown wiki"
git push
```

Keep repo documentation and wiki pages aligned. The README should be the concise entry point; the wiki can hold deeper operational docs.
