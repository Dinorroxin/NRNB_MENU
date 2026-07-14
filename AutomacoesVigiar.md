# Automações Vigiar V1

## Focos de Calor por semana

O programa abre um navegador headless via Playwright e acessa **https://terrabrasilis.dpi.inpe.br/queimadas/bdqueimadas/#graficos**.

Filtros aplicados uma única vez na inicialização:
- Continente: América do Sul
- País: Brasil
- Estado: Rondônia

Filtros aplicados por semana:
- Data de início da semana
- Data de fim da semana

Após aplicar o filtro, expande o acordion `#box-firesByCity` (verifica estado atual antes, evitando recolher se já estiver aberto) e intercepta a resposta da requisição `firesByCity` via `RouteAsync`, extraindo os dados direto do payload.

Os dados são organizados em planilha Excel com as colunas:
- Município
- Regional
- Ano
- Semana
- Data de início da semana
- Data de fim da semana
- Quantidade de focos de calor

Processamento inicia em 2021 até o ano atual. Usa o calendário epidemiológico (SINAN) como referência para segmentação de semanas.

## Previsão de qualidade do ar (AlertAr Saúde)

O programa abre um navegador headless via Playwright e acessa **https://shiny.icict.fiocruz.br/alertarsaude/**.

Aguarda o carregamento da aplicação Shiny, navega até a aba **IQAr**, seleciona o estado de Rondônia (`#uf`) e abre o accordion **Download**. Aguarda o botão de download habilitar e baixa o arquivo CSV.

O arquivo vem com as colunas:
- Município
- Regional
- Data
- Hora
- IQAr
- Classificação (N1 ... N4)
- Qualidade (Boa ... Péssima)

## Previsão de qualidade do ar (SINAM)

### Ainda em desenvolvimento

Abrirá navegador headless via Playwright e acessará **https://data.inpe.br/queimadas/sisam/downloads**, selecionando os filtros do estado de Rondônia, datas de início e fim da análise, e as variáveis químicas (PM2.5, PM10, O3, NO2, SO2, CO).

Após isso, clicará em "Exportar", onde deverá ser inserido um e-mail. O programa acessará o e-mail informado, abrirá o link recebido e baixará o arquivo CSV — que virá em uma pasta zipada. O programa descompactará e organizará os dados em uma planilha Excel com as colunas:
- Município
- Regional
- Data
- Tipo de poluente
- Valores

Arquivos são distribuídos de forma separada, facilitando o tratamento de erros e evitando que uma falha isolada quebre o processo inteiro.

---

Todos os arquivos finais seguem formato longo (poucas colunas, muitas linhas), facilitando manipulação de dados e análises futuras no Power BI.rão no formato longo (Poucas colunas e muitas linhas), facilitando a manipulação dos dados e análises futuras no Power BI.