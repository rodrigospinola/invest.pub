"""Testes unitários para services/fii_csv_service.py."""
import pytest
import sys
from pathlib import Path
from unittest.mock import patch, mock_open

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from services.fii_csv_service import (
    _parse_brl,
    _parse_pct,
    _parse_float,
    _normalize_row,
    load_fiis,
)

# Linha real do CSV do Clube do Valor para MXRF11
SAMPLE_ROW_MXRF11 = [
    "MXRF11", "Papel", "R$ 9,79", "166956.0", "R$ 0,10",
    "1,02%", "3,12%", "6,23%", "12,28%",   # DY mensal, 3M, 6M, 12M
    "1,04%", "1,04%", "1,02%", "12,28%",   # DY médio 3M, 6M, 12M, anual
    "0,00%",                                # vacância
    "1,02%", "5,50%",                       # DY corrente, valorização
    "R$ 3.420.000.000,00", "R$ 9,58", "1,02",  # PL, VP/cota, P/VP
    "0,99%", "-0,50%", "0,49%", "6,00%", "N/A", "5",
]

SAMPLE_ROW_HGLG11 = [
    "HGLG11", "Logística", "R$ 165,00", "84390.0", "R$ 1,25",
    "0,76%", "2,28%", "4,50%", "9,10%",
    "0,76%", "0,75%", "0,76%", "9,10%",
    "2,50%",
    "0,76%", "13,09%",
    "R$ 7.030.000.000,00", "R$ 151,00", "1,09",
    "0,74%", "-3,90%", "-3,18%", "5,74%", "0,00%", "15",
]


# =========================================================
# _parse_brl
# =========================================================

class TestParseBrl:
    def test_valor_simples(self):
        assert _parse_brl("R$ 9,79") == pytest.approx(9.79)

    def test_valor_grande(self):
        assert _parse_brl("R$ 1.121.743.483,26") == pytest.approx(1_121_743_483.26, rel=1e-4)

    def test_valor_com_bilhoes(self):
        assert _parse_brl("R$ 3.420.000.000,00") == pytest.approx(3_420_000_000.0, rel=1e-4)

    def test_na_retorna_none(self):
        assert _parse_brl("N/A") is None

    def test_vazio_retorna_none(self):
        assert _parse_brl("") is None

    def test_traco_retorna_none(self):
        assert _parse_brl("-") is None


# =========================================================
# _parse_pct
# =========================================================

class TestParsePct:
    def test_positivo(self):
        assert _parse_pct("9,44%") == pytest.approx(0.0944)

    def test_negativo(self):
        assert _parse_pct("-5,04%") == pytest.approx(-0.0504)

    def test_zero(self):
        assert _parse_pct("0,00%") == pytest.approx(0.0)

    def test_na_retorna_none(self):
        assert _parse_pct("N/A") is None

    def test_vazio_retorna_none(self):
        assert _parse_pct("") is None

    def test_decimal_ingles(self):
        # Suporta ponto como separador também
        assert _parse_pct("9.44%") == pytest.approx(0.0944)


# =========================================================
# _parse_float
# =========================================================

class TestParseFloat:
    def test_pvp_decimal_brasileiro(self):
        assert _parse_float("0,78") == pytest.approx(0.78)

    def test_pvp_maior_que_1(self):
        assert _parse_float("1,09") == pytest.approx(1.09)

    def test_na_retorna_none(self):
        assert _parse_float("N/A") is None

    def test_vazio_retorna_none(self):
        assert _parse_float("") is None


# =========================================================
# _normalize_row
# =========================================================

class TestNormalizeRow:
    def test_extrai_ticker(self):
        result = _normalize_row(SAMPLE_ROW_MXRF11)
        assert result["ticker"] == "MXRF11"

    def test_extrai_segmento(self):
        result = _normalize_row(SAMPLE_ROW_MXRF11)
        assert result["segmento"] == "Papel"

    def test_extrai_preco(self):
        result = _normalize_row(SAMPLE_ROW_MXRF11)
        assert result["price"] == pytest.approx(9.79)

    def test_extrai_dy_12m_como_decimal(self):
        result = _normalize_row(SAMPLE_ROW_MXRF11)
        assert result["dy"] == pytest.approx(0.1228)

    def test_extrai_pvp(self):
        result = _normalize_row(SAMPLE_ROW_MXRF11)
        assert result["p_vpa"] == pytest.approx(1.02)

    def test_extrai_vacancia_zero(self):
        result = _normalize_row(SAMPLE_ROW_MXRF11)
        assert result["vacancia"] == pytest.approx(0.0)

    def test_extrai_vacancia_positiva(self):
        result = _normalize_row(SAMPLE_ROW_HGLG11)
        assert result["vacancia"] == pytest.approx(0.025)

    def test_volume_em_reais(self):
        # volume = 166956 cotas × R$9,79 ≈ R$1.634.709
        result = _normalize_row(SAMPLE_ROW_MXRF11)
        assert result["volume"] == pytest.approx(166956 * 9.79, rel=0.01)

    def test_extrai_ultimo_dividendo(self):
        result = _normalize_row(SAMPLE_ROW_MXRF11)
        assert result["ultimo_dividendo"] == pytest.approx(0.10)

    def test_extrai_patrimonio_liquido(self):
        result = _normalize_row(SAMPLE_ROW_MXRF11)
        assert result["patrimonio_liq"] == pytest.approx(3_420_000_000.0, rel=1e-4)

    def test_extrai_vp_cota(self):
        result = _normalize_row(SAMPLE_ROW_MXRF11)
        assert result["vp_cota"] == pytest.approx(9.58)

    def test_vacancia_negativa_vira_zero(self):
        row = SAMPLE_ROW_MXRF11.copy()
        row[13] = "-3,50%"
        result = _normalize_row(row)
        assert result["vacancia"] == 0.0

    def test_linha_titulo_retorna_none(self):
        assert _normalize_row(["Lista de FIIs", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""]) is None

    def test_linha_vazia_retorna_none(self):
        assert _normalize_row([]) is None

    def test_linha_curta_retorna_none(self):
        assert _normalize_row(["MXRF11", "Papel"]) is None

    def test_pvp_calculado_quando_ausente(self):
        row = SAMPLE_ROW_MXRF11.copy()
        row[18] = "N/A"  # remove P/VP direto
        result = _normalize_row(row)
        # Deve calcular P/VP = preço / VP = 9.79 / 9.58
        assert result["p_vpa"] == pytest.approx(9.79 / 9.58, rel=0.01)


# =========================================================
# load_fiis
# =========================================================

class TestLoadFiis:
    def _make_csv_content(self, rows: list[list[str]]) -> str:
        import csv, io
        buf = io.StringIO()
        writer = csv.writer(buf)
        writer.writerow(["Lista de FIIs"] + [""] * 25)  # título
        for row in rows:
            writer.writerow(row)
        return buf.getvalue()

    @patch("services.fii_csv_service._find_fiis_csv")
    def test_carrega_fiis_validos(self, mock_find):
        from pathlib import Path
        import tempfile, os
        content = self._make_csv_content([SAMPLE_ROW_MXRF11, SAMPLE_ROW_HGLG11])
        with tempfile.NamedTemporaryFile(mode="w", suffix=".csv", delete=False, encoding="utf-8") as f:
            f.write(content)
            tmp = Path(f.name)
        mock_find.return_value = tmp
        try:
            result = load_fiis()
            assert len(result) == 2
            tickers = [r["ticker"] for r in result]
            assert "MXRF11" in tickers
            assert "HGLG11" in tickers
        finally:
            os.unlink(tmp)

    @patch("services.fii_csv_service._find_fiis_csv")
    def test_retorna_vazio_sem_csv(self, mock_find):
        mock_find.return_value = None
        assert load_fiis() == []

    @patch("services.fii_csv_service._find_fiis_csv")
    def test_filtra_fii_sem_dy_e_pvp(self, mock_find):
        from pathlib import Path
        import tempfile, os
        row_sem_dados = ["XPTO11", "Segmento", "R$ 10,00", "100.0", "R$ 0,00",
                         "N/A", "N/A", "N/A", "N/A",
                         "N/A", "N/A", "N/A", "N/A", "N/A",
                         "N/A", "N/A", "N/A", "N/A", "N/A",
                         "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "0"]
        content = self._make_csv_content([row_sem_dados])
        with tempfile.NamedTemporaryFile(mode="w", suffix=".csv", delete=False, encoding="utf-8") as f:
            f.write(content)
            tmp = Path(f.name)
        mock_find.return_value = tmp
        try:
            result = load_fiis()
            assert result == []
        finally:
            os.unlink(tmp)

    @patch("services.fii_csv_service._find_fiis_csv")
    def test_dy_convertido_para_decimal(self, mock_find):
        from pathlib import Path
        import tempfile, os
        content = self._make_csv_content([SAMPLE_ROW_MXRF11])
        with tempfile.NamedTemporaryFile(mode="w", suffix=".csv", delete=False, encoding="utf-8") as f:
            f.write(content)
            tmp = Path(f.name)
        mock_find.return_value = tmp
        try:
            result = load_fiis()
            assert result[0]["dy"] == pytest.approx(0.1228)
        finally:
            os.unlink(tmp)
