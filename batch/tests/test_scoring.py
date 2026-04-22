"""Testes unitários para services/scoring.py."""
import pytest
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from services.scoring import (
    _clamp,
    _score_pl,
    _score_roe,
    _score_roe_pct,
    _score_roic,
    _score_margem_ebitda,
    _score_cagr,
    _score_div_ebitda,
    _score_dy,
    _score_volume,
    _score_beta,
    _score_pvpa,
    _score_vacancia,
    score_acao,
    score_acao_csv,
    score_fii,
    score_fii_csv,
    score_final,
)


# =========================================================
# _clamp
# =========================================================

class TestClamp:
    def test_valor_dentro_do_intervalo(self):
        assert _clamp(5.0) == 5.0

    def test_valor_abaixo_do_minimo(self):
        assert _clamp(-1.0) == 0.0

    def test_valor_acima_do_maximo(self):
        assert _clamp(11.0) == 10.0

    def test_limites_personalizados(self):
        assert _clamp(15.0, min_val=0.0, max_val=20.0) == 15.0
        assert _clamp(-5.0, min_val=0.0, max_val=20.0) == 0.0
        assert _clamp(25.0, min_val=0.0, max_val=20.0) == 20.0


# =========================================================
# _score_pl
# =========================================================

class TestScorePL:
    def test_pl_none_retorna_zero(self):
        assert _score_pl(None) == 0.0

    def test_pl_negativo_retorna_zero(self):
        assert _score_pl(-5.0) == 0.0

    def test_pl_zero_retorna_zero(self):
        assert _score_pl(0.0) == 0.0

    def test_pl_muito_baixo_retorna_7(self):
        assert _score_pl(2.0) == 7.0

    def test_pl_ideal_minimo_retorna_10(self):
        assert _score_pl(5.0) == 10.0

    def test_pl_ideal_maximo_retorna_7(self):
        assert _score_pl(15.0) == 7.0

    def test_pl_aceitavel_intermediario(self):
        score = _score_pl(20.0)
        assert 4.0 <= score <= 7.0

    def test_pl_caro_baixo_score(self):
        score = _score_pl(30.0)
        assert score <= 4.0

    def test_pl_extremamente_caro(self):
        score = _score_pl(100.0)
        assert score >= 0.0  # nunca negativo

    @pytest.mark.parametrize("pl,expected_range", [
        (5,  (9.5, 10.0)),  # ótimo
        (10, (8.0, 9.0)),   # ótimo
        (15, (6.5, 7.5)),   # limite superior ótimo
        (20, (5.0, 7.0)),   # aceitável
        (25, (3.5, 4.5)),   # caro
    ])
    def test_pl_faixas(self, pl, expected_range):
        score = _score_pl(pl)
        assert expected_range[0] <= score <= expected_range[1]


# =========================================================
# _score_roe
# =========================================================

class TestScoreROE:
    def test_roe_none_retorna_zero(self):
        assert _score_roe(None) == 0.0

    def test_roe_negativo_retorna_zero(self):
        assert _score_roe(-0.05) == 0.0

    def test_roe_excelente_retorna_10(self):
        assert _score_roe(0.30) == 10.0

    def test_roe_acima_30_retorna_10(self):
        assert _score_roe(0.50) == 10.0

    def test_roe_15_pct_retorna_5(self):
        assert _score_roe(0.15) == 5.0

    def test_roe_intermediario(self):
        score = _score_roe(0.20)
        assert 5.0 < score < 10.0


# =========================================================
# _score_dy
# =========================================================

class TestScoreDY:
    def test_dy_none_retorna_zero(self):
        assert _score_dy(None) == 0.0

    def test_dy_zero_retorna_zero(self):
        assert _score_dy(0.0) == 0.0

    def test_dy_negativo_retorna_zero(self):
        assert _score_dy(-0.05) == 0.0

    def test_dy_12_pct_retorna_10(self):
        assert _score_dy(0.12) == 10.0

    def test_dy_acima_12_retorna_10(self):
        assert _score_dy(0.20) == 10.0

    def test_dy_6_pct_retorna_5(self):
        score = _score_dy(0.06)
        assert abs(score - 5.0) < 0.1

    def test_dy_escala_linear(self):
        score_baixo = _score_dy(0.04)
        score_medio = _score_dy(0.08)
        score_alto  = _score_dy(0.12)
        assert score_baixo < score_medio < score_alto


# =========================================================
# _score_volume
# =========================================================

class TestScoreVolume:
    def test_volume_none_retorna_zero(self):
        assert _score_volume(None) == 0.0

    def test_volume_zero_retorna_zero(self):
        assert _score_volume(0) == 0.0

    def test_volume_negativo_retorna_zero(self):
        assert _score_volume(-1000) == 0.0

    def test_volume_alto_retorna_10(self):
        assert _score_volume(100_000_000) == 10.0

    def test_volume_acima_100m_retorna_10(self):
        assert _score_volume(500_000_000) == 10.0

    def test_volume_escala_cresce(self):
        s1 = _score_volume(1_000_000)
        s2 = _score_volume(10_000_000)
        s3 = _score_volume(100_000_000)
        assert s1 < s2 < s3


# =========================================================
# _score_beta
# =========================================================

class TestScoreBeta:
    def test_beta_none_retorna_neutro(self):
        assert _score_beta(None) == 5.0

    def test_beta_baixo_retorna_10(self):
        assert _score_beta(0.3) == 10.0

    def test_beta_meio_retorna_7(self):
        assert _score_beta(1.0) == 7.0

    def test_beta_alto_baixo_score(self):
        score = _score_beta(2.0)
        assert score <= 4.0

    def test_beta_nunca_negativo(self):
        assert _score_beta(10.0) >= 0.0

    def test_beta_inversamente_proporcional(self):
        s_baixo = _score_beta(0.5)
        s_medio = _score_beta(1.0)
        s_alto  = _score_beta(2.0)
        assert s_baixo > s_medio > s_alto


# =========================================================
# _score_pvpa
# =========================================================

class TestScorePVPA:
    def test_pvpa_none_retorna_neutro(self):
        assert _score_pvpa(None) == 5.0

    def test_pvpa_zero_retorna_neutro(self):
        assert _score_pvpa(0.0) == 5.0

    def test_pvpa_negativo_retorna_neutro(self):
        assert _score_pvpa(-0.5) == 5.0

    def test_pvpa_muito_baixo_retorna_10(self):
        assert _score_pvpa(0.7) == 10.0

    def test_pvpa_ideal_retorna_10(self):
        assert _score_pvpa(0.8) == 10.0

    def test_pvpa_alto_baixo_score(self):
        score = _score_pvpa(1.8)
        assert score <= 3.0

    def test_pvpa_inversamente_proporcional(self):
        s1 = _score_pvpa(0.9)
        s2 = _score_pvpa(1.2)
        s3 = _score_pvpa(1.8)
        assert s1 > s2 > s3


# =========================================================
# score_acao
# =========================================================

class TestScoreAcao:
    def _base_info(self, **kwargs) -> dict:
        base = {
            "pl": 10.0,
            "roe": 0.20,
            "dy": 0.06,
            "volume": 50_000_000,
            "beta": 0.8,
        }
        base.update(kwargs)
        return base

    def test_ativo_excelente_score_alto(self):
        info = {
            "pl": 8.0,
            "roe": 0.35,
            "dy": 0.10,
            "volume": 100_000_000,
            "beta": 0.5,
        }
        score = score_acao(info)
        assert score >= 7.0

    def test_ativo_ruim_score_baixo(self):
        info = {
            "pl": -5.0,
            "roe": 0.0,
            "dy": 0.0,
            "volume": 100_000,
            "beta": 3.0,
        }
        score = score_acao(info)
        assert score <= 3.0

    def test_todos_campos_none_retorna_score_valido(self):
        score = score_acao({})
        assert 0.0 <= score <= 10.0

    def test_score_dentro_do_intervalo(self):
        info = self._base_info()
        score = score_acao(info)
        assert 0.0 <= score <= 10.0

    def test_resultado_e_float(self):
        info = self._base_info()
        score = score_acao(info)
        assert isinstance(score, float)

    def test_pl_melhor_aumenta_score(self):
        score_ruim = score_acao(self._base_info(pl=50.0))
        score_bom  = score_acao(self._base_info(pl=10.0))
        assert score_bom > score_ruim


# =========================================================
# score_fii
# =========================================================

class TestScoreFii:
    def _base_info(self, **kwargs) -> dict:
        base = {
            "dy": 0.08,
            "p_vpa": 1.0,
            "volume": 20_000_000,
        }
        base.update(kwargs)
        return base

    def test_fii_excelente_score_alto(self):
        info = {
            "dy": 0.12,
            "p_vpa": 0.85,
            "volume": 100_000_000,
        }
        score = score_fii(info)
        assert score >= 7.0

    def test_fii_ruim_score_baixo(self):
        info = {
            "dy": 0.0,
            "p_vpa": 2.5,
            "volume": 10_000,
        }
        score = score_fii(info)
        assert score <= 3.0

    def test_todos_campos_none_retorna_score_valido(self):
        score = score_fii({})
        assert 0.0 <= score <= 10.0

    def test_score_dentro_do_intervalo(self):
        info = self._base_info()
        score = score_fii(info)
        assert 0.0 <= score <= 10.0

    def test_dy_maior_aumenta_score(self):
        score_baixo = score_fii(self._base_info(dy=0.04))
        score_alto  = score_fii(self._base_info(dy=0.12))
        assert score_alto > score_baixo


# =========================================================
# score_final
# =========================================================

class TestScoreFinal:
    def test_pesos_corretos_70_30(self):
        # 70% quant + 30% qual
        score = score_final(8.0, 6.0)
        expected = 8.0 * 0.7 + 6.0 * 0.3
        assert abs(score - expected) < 0.001

    def test_score_maximo(self):
        score = score_final(10.0, 10.0)
        assert score == 10.0

    def test_score_minimo(self):
        score = score_final(0.0, 0.0)
        assert score == 0.0

    def test_quant_domina_sobre_qual(self):
        # score_quant alto, score_qual baixo → score > 5
        score = score_final(10.0, 0.0)
        assert score == 7.0  # 10*0.7 + 0*0.3

    def test_result_e_float(self):
        assert isinstance(score_final(5.0, 5.0), float)

    @pytest.mark.parametrize("quant,qual,expected", [
        (10.0, 10.0, 10.0),
        (0.0,  0.0,  0.0),
        (7.0,  3.0,  5.8),   # 4.9 + 0.9
        (5.0,  5.0,  5.0),
    ])
    def test_formula_parametrizada(self, quant, qual, expected):
        score = score_final(quant, qual)
        assert abs(score - expected) < 0.01


# =========================================================
# _score_roe_pct (formato CSV — valor em %)
# =========================================================

class TestScoreROEPct:
    def test_none_retorna_zero(self):
        assert _score_roe_pct(None) == 0.0

    def test_negativo_retorna_zero(self):
        assert _score_roe_pct(-5.0) == 0.0

    def test_zero_retorna_zero(self):
        assert _score_roe_pct(0.0) == 0.0

    def test_30_pct_retorna_10(self):
        assert _score_roe_pct(30.0) == 10.0

    def test_acima_30_retorna_10(self):
        assert _score_roe_pct(50.0) == 10.0

    def test_15_pct_retorna_5(self):
        assert _score_roe_pct(15.0) == 5.0

    def test_cresce_com_roe(self):
        s1 = _score_roe_pct(5.0)
        s2 = _score_roe_pct(15.0)
        s3 = _score_roe_pct(30.0)
        assert s1 < s2 < s3

    def test_equivale_score_roe_em_decimal(self):
        # _score_roe(0.20) deve ser equivalente a _score_roe_pct(20.0)
        assert abs(_score_roe_pct(20.0) - _score_roe(0.20)) < 0.001


# =========================================================
# _score_roic
# =========================================================

class TestScoreROIC:
    def test_none_retorna_zero(self):
        assert _score_roic(None) == 0.0

    def test_negativo_retorna_zero(self):
        assert _score_roic(-10.0) == 0.0

    def test_zero_retorna_zero(self):
        assert _score_roic(0.0) == 0.0

    def test_25_pct_retorna_10(self):
        assert _score_roic(25.0) == 10.0

    def test_acima_25_retorna_10(self):
        assert _score_roic(40.0) == 10.0

    def test_entre_15_e_25_retorna_entre_7_e_10(self):
        score = _score_roic(20.0)
        assert 7.0 <= score <= 10.0

    def test_entre_8_e_15_retorna_entre_4_e_7(self):
        score = _score_roic(11.0)
        assert 4.0 <= score <= 7.0

    def test_cresce_com_roic(self):
        s1 = _score_roic(5.0)
        s2 = _score_roic(15.0)
        s3 = _score_roic(25.0)
        assert s1 < s2 < s3


# =========================================================
# _score_margem_ebitda
# =========================================================

class TestScoreMargemEbitda:
    def test_none_retorna_zero(self):
        assert _score_margem_ebitda(None) == 0.0

    def test_negativo_retorna_zero(self):
        assert _score_margem_ebitda(-5.0) == 0.0

    def test_zero_retorna_zero(self):
        assert _score_margem_ebitda(0.0) == 0.0

    def test_acima_30_retorna_10(self):
        assert _score_margem_ebitda(35.0) == 10.0

    def test_30_pct_retorna_10(self):
        assert _score_margem_ebitda(30.0) == 10.0

    def test_entre_20_e_30_retorna_entre_7_e_10(self):
        score = _score_margem_ebitda(25.0)
        assert 7.0 <= score <= 10.0

    def test_entre_10_e_20_retorna_entre_4_e_7(self):
        score = _score_margem_ebitda(15.0)
        assert 4.0 <= score <= 7.0

    def test_cresce_com_margem(self):
        s1 = _score_margem_ebitda(5.0)
        s2 = _score_margem_ebitda(20.0)
        s3 = _score_margem_ebitda(30.0)
        assert s1 < s2 < s3


# =========================================================
# _score_cagr
# =========================================================

class TestScoreCAGR:
    def test_none_retorna_neutro(self):
        assert _score_cagr(None) == 5.0

    def test_negativo_retorna_zero(self):
        assert _score_cagr(-5.0) == 0.0

    def test_zero_retorna_zero(self):
        assert _score_cagr(0.0) == 0.0

    def test_acima_20_retorna_10(self):
        assert _score_cagr(20.0) == 10.0

    def test_30_retorna_10(self):
        assert _score_cagr(30.0) == 10.0

    def test_entre_10_e_20_retorna_entre_7_e_10(self):
        score = _score_cagr(15.0)
        assert 7.0 <= score <= 10.0

    def test_entre_0_e_10_escala_0_a_7(self):
        score = _score_cagr(5.0)
        assert 0.0 <= score <= 7.0

    def test_cresce_com_cagr(self):
        s1 = _score_cagr(3.0)
        s2 = _score_cagr(10.0)
        s3 = _score_cagr(20.0)
        assert s1 < s2 < s3


# =========================================================
# _score_div_ebitda
# =========================================================

class TestScoreDivEbitda:
    def test_none_retorna_neutro(self):
        assert _score_div_ebitda(None) == 5.0

    def test_negativo_retorna_10(self):
        # Empresa com caixa líquido
        assert _score_div_ebitda(-1.0) == 10.0

    def test_zero_retorna_10(self):
        assert _score_div_ebitda(0.0) == 10.0

    def test_entre_0_e_1_5_retorna_entre_7_e_10(self):
        score = _score_div_ebitda(0.75)
        assert 7.0 <= score <= 10.0

    def test_1_5_retorna_7(self):
        score = _score_div_ebitda(1.5)
        assert abs(score - 7.0) < 0.1

    def test_entre_1_5_e_3_retorna_entre_3_e_7(self):
        score = _score_div_ebitda(2.25)
        assert 3.0 <= score <= 7.0

    def test_acima_4_retorna_baixo(self):
        score = _score_div_ebitda(5.0)
        assert score <= 3.0

    def test_nunca_negativo(self):
        assert _score_div_ebitda(100.0) >= 0.0

    def test_inversamente_proporcional(self):
        s1 = _score_div_ebitda(0.5)
        s2 = _score_div_ebitda(2.0)
        s3 = _score_div_ebitda(5.0)
        assert s1 > s2 > s3


# =========================================================
# score_acao_csv
# =========================================================

class TestScoreAcaoCsv:
    def _base_info(self, **kwargs) -> dict:
        base = {
            "pl": 10.0,
            "roe": 18.0,       # % — formato CSV
            "roic": 15.0,      # %
            "margem_ebitda": 22.0,  # %
            "cagr_receita": 12.0,   # %
            "div_ebitda": 1.5,
            "p_vpa": 1.1,
        }
        base.update(kwargs)
        return base

    def test_ativo_excelente_score_alto(self):
        info = {
            "pl": 8.0,
            "roe": 30.0,
            "roic": 25.0,
            "margem_ebitda": 35.0,
            "cagr_receita": 20.0,
            "div_ebitda": 0.0,
            "p_vpa": 0.8,
        }
        score = score_acao_csv(info)
        assert score >= 8.0

    def test_ativo_ruim_score_baixo(self):
        info = {
            "pl": -5.0,
            "roe": 0.0,
            "roic": 0.0,
            "margem_ebitda": 0.0,
            "cagr_receita": 0.0,
            "div_ebitda": 8.0,
            "p_vpa": 3.0,
        }
        score = score_acao_csv(info)
        assert score <= 3.0

    def test_todos_campos_none_retorna_score_valido(self):
        score = score_acao_csv({})
        assert 0.0 <= score <= 10.0

    def test_score_dentro_do_intervalo(self):
        score = score_acao_csv(self._base_info())
        assert 0.0 <= score <= 10.0

    def test_resultado_e_float(self):
        assert isinstance(score_acao_csv(self._base_info()), float)

    def test_pl_melhor_aumenta_score(self):
        score_ruim = score_acao_csv(self._base_info(pl=60.0))
        score_bom  = score_acao_csv(self._base_info(pl=10.0))
        assert score_bom > score_ruim

    def test_roic_maior_aumenta_score(self):
        score_baixo = score_acao_csv(self._base_info(roic=0.0))
        score_alto  = score_acao_csv(self._base_info(roic=30.0))
        assert score_alto > score_baixo

    def test_div_ebitda_menor_aumenta_score(self):
        score_alavancado = score_acao_csv(self._base_info(div_ebitda=5.0))
        score_saudavel   = score_acao_csv(self._base_info(div_ebitda=0.5))
        assert score_saudavel > score_alavancado

    def test_cagr_maior_aumenta_score(self):
        score_sem_crescimento = score_acao_csv(self._base_info(cagr_receita=0.0))
        score_crescimento     = score_acao_csv(self._base_info(cagr_receita=20.0))
        assert score_crescimento > score_sem_crescimento

    def test_pesos_somam_1(self):
        assert abs(0.20 + 0.15 + 0.15 + 0.15 + 0.15 + 0.10 + 0.10 - 1.0) < 1e-9


# =========================================================
# _score_vacancia
# =========================================================

class TestScoreVacancia:
    def test_none_retorna_neutro(self):
        assert _score_vacancia(None) == 5.0

    def test_zero_retorna_10(self):
        assert _score_vacancia(0.0) == 10.0

    def test_negativo_retorna_10(self):
        assert _score_vacancia(-0.01) == 10.0

    def test_5_pct_retorna_entre_7_e_10(self):
        score = _score_vacancia(0.05)
        assert 7.0 <= score <= 10.0

    def test_15_pct_retorna_entre_3_e_7(self):
        score = _score_vacancia(0.15)
        assert 3.0 <= score <= 7.0

    def test_acima_30_retorna_zero(self):
        assert _score_vacancia(0.35) == 0.0

    def test_inversamente_proporcional(self):
        s1 = _score_vacancia(0.0)
        s2 = _score_vacancia(0.10)
        s3 = _score_vacancia(0.25)
        assert s1 > s2 > s3

    def test_nunca_negativo(self):
        assert _score_vacancia(1.0) >= 0.0


# =========================================================
# score_fii_csv
# =========================================================

class TestScoreFiiCsv:
    def _base_info(self, **kwargs) -> dict:
        base = {
            "dy":       0.10,    # 10% DY 12M
            "p_vpa":    0.95,
            "vacancia": 0.02,    # 2%
            "volume":   15_000_000,
        }
        base.update(kwargs)
        return base

    def test_fii_excelente_score_alto(self):
        info = {
            "dy":       0.13,
            "p_vpa":    0.85,
            "vacancia": 0.0,
            "volume":   50_000_000,
        }
        assert score_fii_csv(info) >= 8.0

    def test_fii_ruim_score_baixo(self):
        info = {
            "dy":       0.0,
            "p_vpa":    2.5,
            "vacancia": 0.40,
            "volume":   5_000,
        }
        assert score_fii_csv(info) <= 2.0

    def test_score_dentro_do_intervalo(self):
        score = score_fii_csv(self._base_info())
        assert 0.0 <= score <= 10.0

    def test_resultado_e_float(self):
        assert isinstance(score_fii_csv(self._base_info()), float)

    def test_todos_campos_none_retorna_score_valido(self):
        score = score_fii_csv({})
        assert 0.0 <= score <= 10.0

    def test_dy_maior_aumenta_score(self):
        score_baixo = score_fii_csv(self._base_info(dy=0.04))
        score_alto  = score_fii_csv(self._base_info(dy=0.14))
        assert score_alto > score_baixo

    def test_vacancia_menor_aumenta_score(self):
        score_vazio    = score_fii_csv(self._base_info(vacancia=0.0))
        score_alto_vac = score_fii_csv(self._base_info(vacancia=0.30))
        assert score_vazio > score_alto_vac

    def test_pvp_menor_aumenta_score(self):
        score_desconto = score_fii_csv(self._base_info(p_vpa=0.80))
        score_premium  = score_fii_csv(self._base_info(p_vpa=1.50))
        assert score_desconto > score_premium

    def test_pesos_somam_1(self):
        assert abs(0.35 + 0.30 + 0.20 + 0.15 - 1.0) < 1e-9
