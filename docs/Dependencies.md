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

O `Hud_Principal.csproj` copia `captcha_solver.exe` para a pasta de saída sem condição de existência. 
Se o arquivo não estiver na raiz do projeto, o **build falha** (MSBuild não encontra o arquivo pra copiar) — não há fallback.

---

## Processamento do Relatório GAL (pdfplumber)

O módulo de acompanhamento mensal GAL extrai dados do PDF usando `pdfplumber` (Python). 
extrator é distribuído como `gal_extract.exe` — gerado via PyInstaller e **não está no repositório** (arquivo grande).

### Como gerar o `gal_extract.exe`

**Pré-requisitos:** Python instalado na máquina.

**1. Instale as dependências:**
```
pip install pdfplumber pyinstaller
```

**2. Compile o extrator:**
```
python -m PyInstaller --onefile gal_extract.py
```

**3. Copie o exe gerado para a raiz do projeto:**
```
copy dist\gal_extract.exe gal_extract.exe
```

**4. Rebuilde a solution no Visual Studio.**

O `Hud_Principal.csproj` copia `gal_extract.exe` para a pasta de saída sem condição de existência. 
Se o arquivo não estiver na raiz do projeto, o **build falha** (MSBuild não encontra o arquivo pra copiar) — não há fallback.

---

### Distribuição

Ao distribuir o programa para a equipe, inclua os três arquivos juntos:
```
NRNB_MENU.exe
captcha_solver.exe
gal_extract.exe
```

Ambos `captcha_solver.exe` e `gal_extract.exe` devem estar na **raiz do projeto** antes do build/publish. 
O `Hud_Principal.csproj` copia os dois automaticamente para a pasta de saída — não é necessário copiá-los manualmente após o publish. Os dois são obrigatórios: sem qualquer um deles na raiz, o build falha.