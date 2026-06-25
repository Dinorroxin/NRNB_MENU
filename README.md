## Captcha Solver (ddddocr)

O módulo GAL usa reconhecimento de captcha via `ddddocr` (Python). O solver é distribuído como `captcha_solver.exe` — gerado via PyInstaller e **não está no repositório** (ultrapassa o limite de 100MB do GitHub).

### Como gerar o `captcha_solver.exe`

**Pré-requisitos:** Python instalado na máquina.

**1. Instale as dependências:**
```
pip install ddddocr pyinstaller
```

**2. Descubra onde o `ddddocr` foi instalado:**
```
python -c "import ddddocr, os; print(os.path.dirname(ddddocr.__file__))"
```
Exemplo de saída: `C:\Users\usuario\AppData\Local\...\site-packages\ddddocr`

**3. Compile o solver (substitua o caminho pelo resultado acima):**
```
python -m PyInstaller --onefile --add-data "CAMINHO_DO_DDDDOCR;ddddocr" captcha_solver.py
```

**4. Copie o exe gerado para a raiz do projeto:**
```
copy dist\captcha_solver.exe captcha_solver.exe
```

**5. Rebuilde a solution no Visual Studio.**

### Distribuição

Ao distribuir o programa para a equipe, inclua **ambos os arquivos**:
```
NRNB_MENU.exe
captcha_solver.exe
```

O `captcha_solver.exe` deve ficar na **mesma pasta** do `NRNB_MENU.exe`.

---

## Sobre o config.json

**Localização real do arquivo, durante desenvolvimento:**
```
Hud_Principal/bin/Debug/net10.0-windows/config.json
```

Esse é o arquivo que o programa efetivamente lê e escreve — não o que aparece na raiz dos projetos no Solution Explorer.

**Em produção** (após publicar/distribuir o `.exe`), o arquivo fica sempre **ao lado do executável**:
```
NRNB_MENU/
├── Hud_Principal.exe
├── config.json   ← aqui
```

**Como o arquivo é criado:**
Não precisa criar manualmente. Ao rodar o programa pela primeira vez:
1. Abra "⚙ Configurações"
2. Preencha os campos necessários
3. Clique em Salvar

O `config.json` é gerado automaticamente nesse momento, na pasta correta.

**Referência de estrutura:**
`Modulo_Seguranca/config.template.json` mostra o formato esperado do JSON — serve só de documentação/exemplo, não é lido pelo programa.

---
