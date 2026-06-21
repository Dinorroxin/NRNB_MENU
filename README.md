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
