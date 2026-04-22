"""Testes unitários para jobs/market_data.py."""
import json
import pytest
import sys
from pathlib import Path
from unittest.mock import MagicMock, patch

# Mock de módulos externos antes de importar o módulo alvo
# Evita ModuleNotFoundError em ambiente de teste sem as deps instaladas
_psycopg2_mock = MagicMock()
for _mod in [
    "yfinance",
    "psycopg2",
    "psycopg2.extras",
    "psycopg2.extensions",
]:
    if _mod not in sys.modules:
        sys.modules[_mod] = _psycopg2_mock

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import jobs.market_data as market_data_module
from jobs.market_data import (
    _create_table,
    _upsert_market_data,
    _collect_acoes,
    _collect_fiis,
)


# =========================================================
# Fixtures
# =========================================================

SAMPLE_ACAO_CSV = {
    "ticker": "PETR4.SA",
    "empresa": "Petrobras",
    "setor": "Energia",
    "valor_mercado": 500_000_000_000.0,
    "pl": 4.5,
    "p_vpa": 1.2,
    "lpa": 3.2,
    "roe": 15.3,
    "roic": 12.1,
    "margem_liquida": 18.5,
    "margem_bruta": 45.2,
    "margem_ebitda": 35.1,
    "cagr_lucro": 8.2,
    "cagr_receita": 12.3,
    "div_ebitda": 1.5,
    "liquidez_corrente": 1.8,
}

SAMPLE_FII_INFO = {
    "ticker": "MXRF11.SA",
    "segmento": "Papel",
    "price": 9.79,
    "dy": 0.1228,          # 12.28% → decimal
    "p_vpa": 1.02,
    "volume": 13_710_000,  # R$ 13.71M liquidez diária
    "ultimo_dividendo": 0.10,
    "vacancia": 0.02,      # 2%
    "patrimonio_liq": 4_410_000_000.0,
    "vp_cota": 9.58,
}


# (FIIS hardcoded removido — agora vem do CSV)


# =========================================================
# _create_table
# =========================================================

class TestCreateTable:
    @patch("jobs.market_data.execute_command")
    def test_executa_create_table(self, mock_exec):
        _create_table()
        mock_exec.assert_called_once()
        sql = mock_exec.call_args[0][0]
        assert "CREATE TABLE IF NOT EXISTS market_data" in sql

    @patch("jobs.market_data.execute_command")
    def test_cria_coluna_ticker(self, mock_exec):
        _create_table()
        sql = mock_exec.call_args[0][0]
        assert "ticker" in sql.lower()

    @patch("jobs.market_data.execute_command")
    def test_cria_coluna_dados_jsonb(self, mock_exec):
        _create_table()
        sql = mock_exec.call_args[0][0]
        assert "JSONB" in sql


# =========================================================
# _upsert_market_data
# =========================================================

class TestUpsertMarketData:
    @patch("jobs.market_data.execute_command")
    def test_chama_execute_command(self, mock_exec):
        _upsert_market_data("PETR4.SA", "acao", SAMPLE_ACAO_CSV)
        mock_exec.assert_called_once()

    @patch("jobs.market_data.execute_command")
    def test_passa_ticker_correto(self, mock_exec):
        _upsert_market_data("PETR4.SA", "acao", SAMPLE_ACAO_CSV)
        params = mock_exec.call_args[0][1]
        assert params[0] == "PETR4.SA"

    @patch("jobs.market_data.execute_command")
    def test_passa_tipo_correto(self, mock_exec):
        _upsert_market_data("MXRF11.SA", "fii", SAMPLE_FII_INFO)
        params = mock_exec.call_args[0][1]
        assert params[1] == "fii"

    @patch("jobs.market_data.execute_command")
    def test_serializa_dados_como_json(self, mock_exec):
        _upsert_market_data("PETR4.SA", "acao", SAMPLE_ACAO_CSV)
        params = mock_exec.call_args[0][1]
        dados_json = params[2]
        dados = json.loads(dados_json)
        assert dados["pl"] == SAMPLE_ACAO_CSV["pl"]


# =========================================================
# _collect_acoes (CSV-based)
# =========================================================

class TestCollectAcoes:
    @patch("jobs.market_data._upsert_market_data")
    @patch("jobs.market_data.load_acoes_br")
    def test_coleta_todas_acoes_do_csv(self, mock_csv, mock_upsert):
        acoes = [
            {**SAMPLE_ACAO_CSV, "ticker": "PETR4"},
            {**SAMPLE_ACAO_CSV, "ticker": "VALE3"},
            {**SAMPLE_ACAO_CSV, "ticker": "WEGE3"},
        ]
        mock_csv.return_value = acoes

        sucessos, falhas = _collect_acoes()

        assert len(sucessos) == 3
        assert len(falhas) == 0

    @patch("jobs.market_data._upsert_market_data")
    @patch("jobs.market_data.load_acoes_br")
    def test_adiciona_sufixo_sa_se_ausente(self, mock_csv, mock_upsert):
        mock_csv.return_value = [{**SAMPLE_ACAO_CSV, "ticker": "PETR4"}]

        sucessos, _ = _collect_acoes()

        assert "PETR4.SA" in sucessos

    @patch("jobs.market_data._upsert_market_data")
    @patch("jobs.market_data.load_acoes_br")
    def test_nao_duplica_sufixo_sa(self, mock_csv, mock_upsert):
        mock_csv.return_value = [{**SAMPLE_ACAO_CSV, "ticker": "PETR4.SA"}]

        sucessos, _ = _collect_acoes()

        # Não deve virar PETR4.SA.SA
        assert sucessos[0] == "PETR4.SA"

    @patch("jobs.market_data._upsert_market_data")
    @patch("jobs.market_data.load_acoes_br")
    def test_csv_vazio_retorna_falha(self, mock_csv, mock_upsert):
        mock_csv.return_value = []

        sucessos, falhas = _collect_acoes()

        assert len(sucessos) == 0
        assert len(falhas) > 0

    @patch("jobs.market_data._upsert_market_data")
    @patch("jobs.market_data.load_acoes_br")
    def test_ignora_ativo_sem_ticker(self, mock_csv, mock_upsert):
        mock_csv.return_value = [{"ticker": "", "pl": 10.0}]

        sucessos, _ = _collect_acoes()

        assert len(sucessos) == 0
        mock_upsert.assert_not_called()

    @patch("jobs.market_data._upsert_market_data")
    @patch("jobs.market_data.load_acoes_br")
    def test_tipo_passado_e_acao(self, mock_csv, mock_upsert):
        mock_csv.return_value = [{**SAMPLE_ACAO_CSV, "ticker": "PETR4"}]

        _collect_acoes()

        tipo = mock_upsert.call_args[0][1]
        assert tipo == "acao"

    @patch("jobs.market_data._upsert_market_data")
    @patch("jobs.market_data.load_acoes_br")
    def test_tolerancia_a_excecao_no_upsert(self, mock_csv, mock_upsert):
        mock_csv.return_value = [
            {**SAMPLE_ACAO_CSV, "ticker": "PETR4"},
            {**SAMPLE_ACAO_CSV, "ticker": "VALE3"},
        ]
        mock_upsert.side_effect = [Exception("DB error"), None]

        sucessos, falhas = _collect_acoes()

        assert len(falhas) == 1
        assert len(sucessos) == 1


# =========================================================
# _collect_fiis (yfinance — inalterado)
# =========================================================

class TestCollectFiis:
    @patch("jobs.market_data._upsert_market_data")
    @patch("jobs.market_data.load_fiis")
    def test_coleta_fiis_do_csv(self, mock_csv, mock_upsert):
        mock_csv.return_value = [
            {**SAMPLE_FII_INFO, "ticker": "MXRF11"},
            {**SAMPLE_FII_INFO, "ticker": "HGLG11"},
        ]
        sucessos, falhas = _collect_fiis()
        assert len(sucessos) == 2
        assert len(falhas) == 0

    @patch("jobs.market_data._upsert_market_data")
    @patch("jobs.market_data.load_fiis")
    def test_csv_vazio_retorna_falha(self, mock_csv, mock_upsert):
        mock_csv.return_value = []
        sucessos, falhas = _collect_fiis()
        assert len(sucessos) == 0
        assert len(falhas) > 0

    @patch("jobs.market_data._upsert_market_data")
    @patch("jobs.market_data.load_fiis")
    def test_adiciona_sufixo_sa(self, mock_csv, mock_upsert):
        mock_csv.return_value = [{**SAMPLE_FII_INFO, "ticker": "MXRF11"}]
        sucessos, _ = _collect_fiis()
        assert "MXRF11.SA" in sucessos

    @patch("jobs.market_data._upsert_market_data")
    @patch("jobs.market_data.load_fiis")
    def test_tipo_passado_e_fii(self, mock_csv, mock_upsert):
        mock_csv.return_value = [{**SAMPLE_FII_INFO, "ticker": "MXRF11"}]
        _collect_fiis()
        tipo = mock_upsert.call_args[0][1]
        assert tipo == "fii"

    @patch("jobs.market_data._upsert_market_data")
    @patch("jobs.market_data.load_fiis")
    def test_tolerancia_excecao_upsert(self, mock_csv, mock_upsert):
        mock_csv.return_value = [
            {**SAMPLE_FII_INFO, "ticker": "MXRF11"},
            {**SAMPLE_FII_INFO, "ticker": "HGLG11"},
        ]
        mock_upsert.side_effect = [Exception("DB error"), None]
        sucessos, falhas = _collect_fiis()
        assert len(falhas) == 1
        assert len(sucessos) == 1


# =========================================================
# main
# =========================================================

class TestMain:
    @patch("jobs.market_data._collect_fiis")
    @patch("jobs.market_data._collect_acoes")
    @patch("jobs.market_data._create_table")
    def test_sucesso_sem_exit(self, mock_table, mock_acoes, mock_fiis):
        mock_acoes.return_value = (["PETR4.SA", "VALE3.SA"], [])
        mock_fiis.return_value = (["MXRF11.SA"], [])

        market_data_module.main()  # não deve lançar SystemExit

        mock_table.assert_called_once()
        mock_acoes.assert_called_once()
        mock_fiis.assert_called_once()

    @patch("jobs.market_data._collect_fiis")
    @patch("jobs.market_data._collect_acoes")
    @patch("jobs.market_data._create_table")
    def test_sem_coleta_exit_1(self, mock_table, mock_acoes, mock_fiis):
        mock_acoes.return_value = ([], ["CSV_LOAD_FAILED"])
        mock_fiis.return_value = ([], ["CSV_FIIS_LOAD_FAILED"])

        with pytest.raises(SystemExit) as exc_info:
            market_data_module.main()

        assert exc_info.value.code == 1

    @patch("jobs.market_data._collect_fiis")
    @patch("jobs.market_data._collect_acoes")
    @patch("jobs.market_data._create_table")
    def test_falhas_parciais_nao_exit(self, mock_table, mock_acoes, mock_fiis):
        mock_acoes.return_value = (["PETR4.SA", "VALE3.SA"], ["MGLU3.SA"])
        mock_fiis.return_value = (["MXRF11.SA"], [])

        market_data_module.main()  # não deve lançar SystemExit

    @patch("jobs.market_data._collect_fiis")
    @patch("jobs.market_data._collect_acoes")
    @patch("jobs.market_data._create_table")
    def test_so_acoes_com_sucesso_nao_exit(self, mock_table, mock_acoes, mock_fiis):
        mock_acoes.return_value = (["PETR4.SA"], [])
        mock_fiis.return_value = ([], ["CSV_FIIS_LOAD_FAILED"])

        market_data_module.main()  # não deve lançar SystemExit
