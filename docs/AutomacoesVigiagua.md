# Automações Sisagua V1

## Diretriz Nacional Mensal e Anual

Ambas automações consistem no mesmo processo mas apenas um campo com seleções diferentes. Os passos dessa automação são

1. Abrir navegador e acessar **https://sisagua.saude.gov.br/sisagua/paginaExterna.jsf**
1. Clicar no botão "Entrar"
1. Escrever as credenciais informadas nas configurações e clicar no botão "Entrar"
1. Acessar a url **https://sisagua.saude.gov.br/sisagua/paginas/seguro/relatorioDiretrizNacional/relDiretrizNacionalParametrosBasicos.jsf?faces-redirect=true**
1. Selecionar os parâmetros
    * Abrangência (Unidade Federativa para o estado todo)
    * Período (Será Mensal ou Anual dependendo de qual automação você está realizando)
    * Parâmetro (CRL, Turbidez e Coliformes Totais/E coli)
1. Após isso ele realizará o tratamento dos dados e retornará as tabelas separadas com as seguintes colunas
    * Município
    * Código IBGE
    * População
    * Regional
    * Período
    * Percentual
    * N
    * Parâmetro

# Automações GAL (Gerenciador de Ambiente Laboratorial)

## Relatório Mensal

Os passos dessa automação são:

1. Abrir navegador e acessar o sistema GAL de Rondônia
1. Selecionar a referência do relatório (Data de Cadastro ou de Coleta) e o intervalo de data início/fim
1. Informar o código IBGE do município (Por padrão será 11, referenciando o estado todo)
1. Resolver o captcha exibido pelo sistema
1. Submeter a consulta e aguardar geração do relatório em PDF
1. O sistema abre uma nova aba que dispara `window.print()` automaticamente ao carregar — esse comportamento é neutralizado antes de qualquer página abrir, evitando interrupção do fluxo
1. Baixar o PDF e extrair os dados via leitura de coordenadas do texto no documento
1. Organizar os dados em planilha Excel, extraindo as colunas TOT/SAT/INS do intervalo de tempo selecionado. **IMPORTANTE** ressaltar que devido à forma que o gal entrega o arquivo, **NÃO** realizar coleta de mais de um ano por processo
1. Após a coleta, ele tratará os dados e retornará com as seguintes colunas
    * Município
    * Regional
    * Ano
    * Mês
    * TOT (Total)
    * SAT (Satisfatório)
    * INS (Insatisfatório)

## Captcha

O sistema GAL exige resolução de captcha (padrão numérico/letra, texto laranja sobre fundo branco) a cada consulta, sem alternativa de bypass pela interface.

Não temos permissão de administrador nas máquinas onde o programa roda, o que impede a instalação de bibliotecas/frameworks de reconhecimento de captcha diretamente no ambiente Python padrão. Para não depender disso nem exigir que o usuário final instale nada externamente, a resolução foi resolvida via um executável standalone (`captcha_solver.exe`), gerado a partir de um script Python já existente (reaproveitado de um projeto anterior) e compilado com PyInstaller — rodando de forma independente, sem precisar de Python instalado na máquina do usuário.

## Extração de PDF

O relatório baixado em PDF é processado por `gal_extract.exe`, um executável Python (biblioteca `pdfplumber`) também compilado com PyInstaller — mesma justificativa técnica do captcha solver: sem permissão admin pra instalar dependências Python no ambiente do usuário final.

O script extrai as tabelas de cada página do PDF, remove cabeçalhos duplicados (quando o relatório quebra em múltiplas páginas) e remove sub-cabeçalhos intermediários (`TOT`/`SAT`/`INS`), retornando os dados limpos em formato JSON.

O build detecta automaticamente se `gal_extract.exe` está presente e o copia para a pasta de saída. Caso não esteja, usa `gal_extract.py` como fallback — exigindo Python instalado na máquina alvo.

## Distribuição

Nenhum dos dois `.exe` (`captcha_solver.exe`, `gal_extract.exe`) é distribuído pelo repositório GitHub — ambos ultrapassam o limite de 100MB por arquivo da plataforma. São distribuídos separadamente e devem ficar na **mesma pasta** do executável principal (`NRNB_MENU.exe`):

---

Todos os arquivos finais seguem formato longo (poucas colunas, muitas linhas), facilitando manipulação de dados e análises futuras no Power BI.