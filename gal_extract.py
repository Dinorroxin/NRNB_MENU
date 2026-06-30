#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
Extrai a tabela do relatório GAL mensal usando pdfplumber.
Saída: JSON para stdout (lista de listas de strings).
Linha 0 = cabeçalho (com meses), linhas seguintes = dados.
"""
import sys
import json
import pdfplumber

def limpar(c):
    if c is None:
        return ''
    return str(c).replace('\n', ' ').strip()

TSI = {'TOT', 'SAT', 'INS', 'IN', ''}

def eh_linha_tsi(row):
    """True se a linha for apenas o sub-cabeçalho TOT/SAT/INS."""
    nao_vazios = [c for c in row if c]
    return bool(nao_vazios) and all(c in TSI for c in nao_vazios)

def main():
    if len(sys.argv) < 2:
        sys.stderr.write('Uso: gal_extract.py <caminho_pdf>\n')
        sys.exit(1)

    path = sys.argv[1]

    try:
        all_rows = []
        with pdfplumber.open(path) as pdf:
            for page in pdf.pages:
                table = page.extract_table()
                if table:
                    all_rows.extend(
                        [[limpar(c) for c in row] for row in table]
                    )
    except Exception as e:
        sys.stderr.write(f'Erro ao abrir PDF: {e}\n')
        sys.exit(1)

    # Remove linhas completamente vazias
    all_rows = [r for r in all_rows if any(r)]

    if not all_rows:
        sys.stderr.write('Nenhuma tabela encontrada no PDF\n')
        sys.exit(1)

    primeira = all_rows[0]
    resultado = [primeira]

    for row in all_rows[1:]:
        if row == primeira:        # cabeçalho repetido de nova página
            continue
        if eh_linha_tsi(row):      # sub-cabeçalho TOT/SAT/INS
            continue
        resultado.append(row)

    json.dump(resultado, sys.stdout, ensure_ascii=False)

if __name__ == '__main__':
    main()
