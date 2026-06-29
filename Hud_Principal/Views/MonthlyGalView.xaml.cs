using Conversor_de_Arquivos;
using Modulo_Seguranca;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WebAutomation;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace Hud_Principal.Views
{
    public partial class MonthlyGalView : BaseView
    {
        private bool _formattingDate;

        public MonthlyGalView()
        {
            InitializeComponent();
            TxtDataInicio.Text = $"01/01/{DateTime.Now.Year}";
            TxtDataFim.Text    = DateTime.Now.ToString("dd/MM/yyyy");
        }

        // ── Date mask helpers ───────────────────────────────────────────────────

        // Block every non-digit character; "/" is auto-inserted by Date_TextChanged.
        private void Date_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!e.Text.All(char.IsDigit))
                e.Handled = true;
        }

        // After each keystroke, strip non-digits and rebuild "dd/MM/yyyy" layout.
        private void Date_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_formattingDate) return;
            var tb = (WpfTextBox)sender;

            string digits = new string(tb.Text.Where(char.IsDigit).ToArray());
            if (digits.Length > 8) digits = digits[..8];

            string formatted = digits.Length switch
            {
                0 or 1 or 2 => digits,
                3 or 4       => digits[..2] + "/" + digits[2..],
                _            => digits[..2] + "/" + digits[2..4] + "/" + digits[4..]
            };

            _formattingDate = true;
            int caret = tb.CaretIndex;
            tb.Text = formatted;
            // Keep caret roughly in place, advancing past slashes that were just inserted
            int newCaret = Math.Min(caret, formatted.Length);
            while (newCaret < formatted.Length && formatted[newCaret] == '/') newCaret++;
            tb.CaretIndex = newCaret;
            _formattingDate = false;

            // Clear any previous error highlight while the user is still typing
            tb.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
        }

        // Validate the final date when the field loses focus.
        private void Date_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = (WpfTextBox)sender;
            if (!DateTime.TryParseExact(tb.Text, "dd/MM/yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                tb.BorderBrush = WpfBrushes.Red;
                TxtStatus.Text = $"Data inválida: '{tb.Text}'. Use o formato dd/MM/aaaa.";
            }
            else
            {
                tb.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
            }
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtDataInicio.Text) ||
                string.IsNullOrWhiteSpace(TxtDataFim.Text) ||
                string.IsNullOrWhiteSpace(TxtIbge.Text))
            {
                TxtStatus.Text = "Preencha todos os campos antes de iniciar.";
                return;
            }

            var config = new ConfiguracaoService().Carregar();

            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(config.Gal.Usuario))
                errors.Add("Usuário do GAL não configurado.");
            if (string.IsNullOrWhiteSpace(config.Gal.Senha))
                errors.Add("Senha do GAL não configurada.");
            if (string.IsNullOrWhiteSpace(config.Gal.PastaBrutaRelatoriosMensalGal))
                errors.Add("Pasta de download do GAL não configurada.");
            if (string.IsNullOrWhiteSpace(config.Gal.PastaRelatoriosMensalGal))
                errors.Add("Pasta de relatórios processados do GAL não configurada.");

            if (errors.Count > 0)
            {
                TxtStatus.Text = string.Join("\n", errors);
                return;
            }

            // Lê todos os valores de UI antes do Task.Run
            string referencia  = ((ComboBoxItem)CmbReferencia.SelectedItem).Content.ToString()!;
            string dataInicio  = TxtDataInicio.Text;
            string dataFim     = TxtDataFim.Text;
            string ibge        = TxtIbge.Text;
            string usuario     = config.Gal.Usuario;
            string senha       = config.Gal.Senha;
            string modulo      = config.Gal.Modulo;
            string laboratorio = config.Gal.Laboratorio;
            string pastaBruta  = config.Gal.PastaBrutaRelatoriosMensalGal;
            string pastaMestre = config.Gal.PastaRelatoriosMensalGal;

            BtnStart.IsEnabled = false;

            var progress = new Progress<string>(msg => TxtStatus.Text = msg);

            // 1. Download do PDF via Playwright
            bool sucesso = await Task.Run(() =>
                new GalMonthlyAutomation().BaixarRelatorioMensalAsync(
                    usuario, senha, modulo, laboratorio,
                    pastaBruta, referencia, dataInicio, dataFim, ibge,
                    progress));

            if (!sucesso)
            {
                TxtStatus.Text = "Falha na coleta. Verifique o status acima.";
                BtnStart.IsEnabled = true;
                return;
            }

            // 2. Processa o PDF baixado e faz append/deduplicação no mestre
            string dIni     = dataInicio.Replace("/", "");
            string dFim     = dataFim.Replace("/", "");
            string pathPdf  = Path.Combine(pastaBruta,  $"Relatorio_GAL_{dIni}_a_{dFim}.pdf");
            string pathMestre = Path.Combine(pastaMestre, "Acompanhamento_Mensal_Agua_MESTRE.xlsx");

            var resultado = await Task.Run(() =>
                new GalMonthlyDataProcessor().ProcessarAsync(pathPdf, pathMestre, progress));

            TxtStatus.Text = resultado.Sucesso
                ? $"Concluído. {resultado.MunicipiosEncontrados} municípios processados, " +
                  $"{resultado.LinhasProcessadas} linhas no mestre."
                : $"Download OK, mas falha no processamento: {resultado.Erro}";

            BtnStart.IsEnabled = true;
        }
    }
}