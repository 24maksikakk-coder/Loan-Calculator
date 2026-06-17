// LoanCalculator.cs - Кредитный калькулятор на C# (CLI + WinForms)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text.Json;

namespace LoanCalculator
{
    public class PaymentRow
    {
        public int Month { get; set; }
        public double Payment { get; set; }
        public double Principal { get; set; }
        public double Interest { get; set; }
        public double Balance { get; set; }
    }

    public class Calculator
    {
        public static (List<PaymentRow> schedule, double totalPaid, double overpayment) Calculate(
            double amount, double annualRate, int term, string type)
        {
            var schedule = new List<PaymentRow>();
            double balance = amount;
            double totalPaid = 0;
            double monthlyRate = annualRate / 100 / 12;
            double payment, principal, interest;

            if (type.ToLower() == "annuity")
            {
                if (monthlyRate == 0) payment = amount / term;
                else
                {
                    double pow = Math.Pow(1 + monthlyRate, term);
                    payment = amount * monthlyRate * pow / (pow - 1);
                }
                for (int month = 1; month <= term; month++)
                {
                    interest = balance * monthlyRate;
                    principal = payment - interest;
                    if (principal > balance) { principal = balance; payment = principal + interest; }
                    balance -= principal;
                    totalPaid += payment;
                    schedule.Add(new PaymentRow { Month = month, Payment = payment, Principal = principal, Interest = interest, Balance = balance });
                }
            }
            else // diff
            {
                double principalPerMonth = amount / term;
                for (int month = 1; month <= term; month++)
                {
                    interest = balance * monthlyRate;
                    payment = principalPerMonth + interest;
                    if (payment > balance + interest) { payment = balance + interest; principalPerMonth = balance; }
                    balance -= principalPerMonth;
                    totalPaid += payment;
                    schedule.Add(new PaymentRow { Month = month, Payment = payment, Principal = principalPerMonth, Interest = interest, Balance = balance });
                }
            }
            double overpayment = totalPaid - amount;
            return (schedule, totalPaid, overpayment);
        }

        public static void PrintSchedule(List<PaymentRow> schedule)
        {
            Console.WriteLine("\nГрафик платежей:");
            Console.WriteLine($"{"№",-4} {"Платёж",-12} {"Основной долг",-15} {"Проценты",-12} {"Остаток",-12}");
            foreach (var r in schedule)
                Console.WriteLine($"{r.Month,-4} {r.Payment,10:F2} {r.Principal,14:F2} {r.Interest,11:F2} {r.Balance,11:F2}");
        }

        public static void ExportCSV(string filepath, List<PaymentRow> schedule)
        {
            using (var sw = new StreamWriter(filepath))
            {
                sw.WriteLine("month,payment,principal,interest,balance");
                foreach (var r in schedule)
                    sw.WriteLine($"{r.Month},{r.Payment:F2},{r.Principal:F2},{r.Interest:F2},{r.Balance:F2}");
            }
        }
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--gui")
            {
                Application.EnableVisualStyles();
                Application.Run(new LoanCalculatorGUI());
                return;
            }
            // CLI
            try
            {
                double amount = 0, rate = 0;
                int term = 0;
                string type = "annuity";
                bool showSchedule = false;
                string output = "";
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--amount": amount = double.Parse(args[++i]); break;
                        case "--rate": rate = double.Parse(args[++i]); break;
                        case "--term": term = int.Parse(args[++i]); break;
                        case "--type": type = args[++i]; break;
                        case "--schedule": showSchedule = true; break;
                        case "--output": output = args[++i]; break;
                    }
                }
                if (amount == 0 || rate == 0 || term == 0)
                {
                    InteractiveMode();
                    return;
                }
                var (schedule, total, overpayment) = Calculator.Calculate(amount, rate, term, type);
                Console.WriteLine($"\nСумма кредита: {amount:F2}");
                Console.WriteLine($"Ставка: {rate:F2}% годовых");
                Console.WriteLine($"Срок: {term} месяцев");
                Console.WriteLine($"Тип платежа: {(type.ToLower() == "annuity" ? "Аннуитетный" : "Дифференцированный")}");
                Console.WriteLine($"Общая сумма выплат: {total:F2}");
                Console.WriteLine($"Переплата: {overpayment:F2}");
                if (showSchedule) Calculator.PrintSchedule(schedule);
                if (!string.IsNullOrEmpty(output))
                {
                    Calculator.ExportCSV(output, schedule);
                    Console.WriteLine($"График сохранён в {output}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка: {e.Message}");
            }
        }

        static void InteractiveMode()
        {
            Console.WriteLine("Интерактивный режим кредитного калькулятора");
            Console.Write("Сумма кредита: ");
            double amount = double.Parse(Console.ReadLine());
            Console.Write("Годовая ставка (%): ");
            double rate = double.Parse(Console.ReadLine());
            Console.Write("Срок (месяцев): ");
            int term = int.Parse(Console.ReadLine());
            Console.Write("Тип платежа (annuity/diff): ");
            string type = Console.ReadLine().Trim().ToLower();
            if (type != "diff") type = "annuity";
            var (schedule, total, overpayment) = Calculator.Calculate(amount, rate, term, type);
            Console.WriteLine($"\nСумма кредита: {amount:F2}");
            Console.WriteLine($"Ставка: {rate:F2}%");
            Console.WriteLine($"Срок: {term} мес.");
            Console.WriteLine($"Тип: {(type == "annuity" ? "Аннуитетный" : "Дифференцированный")}");
            Console.WriteLine($"Общая сумма выплат: {total:F2}");
            Console.WriteLine($"Переплата: {overpayment:F2}");
            Console.Write("Показать график платежей? (y/n): ");
            if (Console.ReadLine().Trim().ToLower() == "y") Calculator.PrintSchedule(schedule);
            Console.Write("Сохранить график в CSV? (y/n): ");
            if (Console.ReadLine().Trim().ToLower() == "y")
            {
                Console.Write("Имя файла (по умолчанию schedule.csv): ");
                string fname = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(fname)) fname = "schedule.csv";
                Calculator.ExportCSV(fname, schedule);
                Console.WriteLine($"Сохранено в {fname}");
            }
        }
    }

    // ========== GUI ==========
    public class LoanCalculatorGUI : Form
    {
        private TextBox amountBox, rateBox, termBox;
        private ComboBox typeCombo;
        private TextBox resultBox;

        public LoanCalculatorGUI()
        {
            Text = "Кредитный калькулятор";
            Size = new System.Drawing.Size(700, 500);
            StartPosition = FormStartPosition.CenterScreen;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3, Padding = new Padding(10) };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            layout.Controls.Add(new Label { Text = "Сумма кредита:", AutoSize = true }, 0, 0);
            amountBox = new TextBox { Width = 100 };
            layout.Controls.Add(amountBox, 1, 0);
            layout.Controls.Add(new Label { Text = "Ставка (%):", AutoSize = true }, 2, 0);
            rateBox = new TextBox { Width = 80 };
            layout.Controls.Add(rateBox, 3, 0);

            layout.Controls.Add(new Label { Text = "Срок (мес.):", AutoSize = true }, 0, 1);
            termBox = new TextBox { Width = 80 };
            layout.Controls.Add(termBox, 1, 1);
            layout.Controls.Add(new Label { Text = "Тип:", AutoSize = true }, 2, 1);
            typeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Items = { "annuity", "diff" }, SelectedIndex = 0 };
            layout.Controls.Add(typeCombo, 3, 1);

            var calcBtn = new Button { Text = "Рассчитать", Dock = DockStyle.Fill };
            calcBtn.Click += (s, e) => Calculate();
            layout.Controls.Add(calcBtn, 0, 2);
            layout.SetColumnSpan(calcBtn, 4);

            resultBox = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
            layout.Controls.Add(resultBox, 0, 3);
            layout.SetColumnSpan(resultBox, 4);

            var saveBtn = new Button { Text = "Сохранить CSV" };
            saveBtn.Click += (s, e) => SaveCSV();
            layout.Controls.Add(saveBtn, 0, 4);
            layout.SetColumnSpan(saveBtn, 4);

            Controls.Add(layout);
        }

        private void Calculate()
        {
            try
            {
                double amount = double.Parse(amountBox.Text);
                double rate = double.Parse(rateBox.Text);
                int term = int.Parse(termBox.Text);
                string type = typeCombo.SelectedItem.ToString();
                var (schedule, total, overpayment) = Calculator.Calculate(amount, rate, term, type);
                resultBox.Text = "";
                resultBox.AppendText($"Сумма кредита: {amount:F2}\n");
                resultBox.AppendText($"Ставка: {rate:F2}% годовых\n");
                resultBox.AppendText($"Срок: {term} месяцев\n");
                resultBox.AppendText($"Тип: {(type == "annuity" ? "Аннуитетный" : "Дифференцированный")}\n");
                resultBox.AppendText($"Общая сумма выплат: {total:F2}\n");
                resultBox.AppendText($"Переплата: {overpayment:F2}\n\n");
                resultBox.AppendText($"{"№",-4} {"Платёж",-12} {"Основной долг",-15} {"Проценты",-12} {"Остаток",-12}\n");
                foreach (var r in schedule)
                    resultBox.AppendText($"{r.Month,-4} {r.Payment,10:F2} {r.Principal,14:F2} {r.Interest,11:F2} {r.Balance,11:F2}\n");
                Tag = schedule;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void SaveCSV()
        {
            var schedule = Tag as List<PaymentRow>;
            if (schedule == null || !schedule.Any())
            {
                MessageBox.Show("Сначала выполните расчёт");
                return;
            }
            var sfd = new SaveFileDialog { Filter = "CSV files|*.csv", DefaultExt = "csv" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                Calculator.ExportCSV(sfd.FileName, schedule);
                MessageBox.Show("Сохранено");
            }
        }
    }
}
