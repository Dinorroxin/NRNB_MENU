# NRNB_MENU

## **Este projeto é para o Estado de Rondônia, não deve ser usado para outros fins além de estudo e melhorias futuras**

### Ideia
Este menu tem o objetivo de automatizar a coleta e tratamento de dados contínuos, reduzindo trabalho manual, padronizando dados e evitando erros humanos.

Atualmente o projeto possui somente automações de web scraping, mas com visão para implementação futura de APIs públicas.

---

## Arquitetura

A solução é dividida em 4 projetos:

| Projeto | Descrição | Framework |
|---|---|---|
| `Hud_Principal` | Frontend WPF | `net10.0-windows` |
| `Modulo_Seguranca` | Configuração e segurança | `net10.0` |
| `Conversor_de_Arquivos` | Processamento de dados | `net10.0` |
| `WebAutomation` | Automação de navegador (Playwright) | `net8.0` (fixado por bug confirmado de cópia de DLL no net10.0) |

---

## Programas-alvo
- **Vigiagua** - Programa Nacional de Vigilância da Qualidade da Água para Consumo Humano
- **Vigiar** - Vigilância em Saúde de Populações Expostas a Poluição Atmosférica
- **Vigidesastres** - Programa de Vigilância em Saúde Ambiental Relacionada aos Riscos Decorrentes dos Desastres
- **Vigipeq** - Vigilânia em Saúde de Populações Expostas a Contaminantes Químicos

---
## Sistemas-alvo

- **Sisagua** - qualidade da água
- **Observatório de Clima e Saúde** - qualidade do ar
- **BdQueimadas** - focos de calor
- **GAL** - laboratório
- **SISAM** - previsão de qualidade do ar (INPE)

---

## Status atual

**Concluído:**
- Vigiagua - Diretriz (mensal e anual)
- Vigiar - Focos de Calor
- Vigiar - IQAr
- GAL - Relatório de amostras

**Em andamento:**
- SISAM (próximo módulo planejado)
- Testes ao vivo do IQAr

**Pendente:**
- Idaron
- Vigidesastres
- Vigipeq
- Reconstrução do Power BI

---

## Tecnologias

- .NET 10 / .NET 8
- WPF
- Microsoft Playwright
- EPPlus (Excel)
- UglyToad.PdfPig (PDF)
- pdfplumber (PDF)
- Python/ddddocr (captcha, compilado via PyInstaller)

---

## Requisitos

- Visual Studio 2022+
- .NET SDK 10 e .NET SDK 8

Usuários finais não precisam instalar nada além do executável — build self-contained, zero downloads externos.

---

## Roadmap

- Finalizar módulo SISAM
- Implementar Idaron, Vigidesastres, Vigipeq
- Reconstruir dashboards Power BI
- Passe de polimento visual/UX

---

## Autor

Felipe Siqueira Ramos Galvez
AGEVISA / GTVAM-NRNB — Rondônia
