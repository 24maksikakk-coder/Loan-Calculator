// LoanCalculator.java - Кредитный калькулятор на Java (CLI + Swing GUI)
import javax.swing.*;
import javax.swing.table.DefaultTableModel;
import java.awt.*;
import java.awt.event.*;
import java.io.*;
import java.nio.file.*;
import java.util.*;
import java.util.List;
import java.text.DecimalFormat;

public class LoanCalculator {
    static class PaymentRow {
        int month; double payment, principal, interest, balance;
        PaymentRow(int m, double p, double pr, double i, double b) {
            month = m; payment = p; principal = pr; interest = i; balance = b;
        }
    }

    static class CalcResult {
        List<PaymentRow> schedule;
        double totalPaid, overpayment;
        CalcResult(List<PaymentRow> s, double t, double o) { schedule = s; totalPaid = t; overpayment = o; }
    }

    public static CalcResult calculate(double amount, double annualRate, int term, String type) {
        List<PaymentRow> schedule = new ArrayList<>();
        double balance = amount;
        double totalPaid = 0;
        double monthlyRate = annualRate / 100 / 12;
        double payment, principal, interest;

        if (type.equalsIgnoreCase("annuity")) {
            if (monthlyRate == 0) payment = amount / term;
            else {
                double pow = Math.pow(1 + monthlyRate, term);
                payment = amount * monthlyRate * pow / (pow - 1);
            }
            for (int month = 1; month <= term; month++) {
                interest = balance * monthlyRate;
                principal = payment - interest;
                if (principal > balance) { principal = balance; payment = principal + interest; }
                balance -= principal;
                totalPaid += payment;
                schedule.add(new PaymentRow(month, payment, principal, interest, balance));
            }
        } else { // diff
            double principalPerMonth = amount / term;
            for (int month = 1; month <= term; month++) {
                interest = balance * monthlyRate;
                payment = principalPerMonth + interest;
                if (payment > balance + interest) { payment = balance + interest; principalPerMonth = balance; }
                balance -= principalPerMonth;
                totalPaid += payment;
                schedule.add(new PaymentRow(month, payment, principalPerMonth, interest, balance));
            }
        }
        double overpayment = totalPaid - amount;
        return new CalcResult(schedule, totalPaid, overpayment);
    }

    public static void printSchedule(List<PaymentRow> schedule) {
        System.out.println("\nГрафик платежей:");
        System.out.printf("%-4s %-12s %-15s %-12s %-12s\n", "№", "Платёж", "Основной долг", "Проценты", "Остаток");
        for (PaymentRow r : schedule) {
            System.out.printf("%-4d %10.2f %14.2f %11.2f %11.2f\n", r.month, r.payment, r.principal, r.interest, r.balance);
        }
    }

    public static void exportCSV(String filepath, List<PaymentRow> schedule) throws IOException {
        try (PrintWriter pw = new PrintWriter(filepath)) {
            pw.println("month,payment,principal,interest,balance");
            for (PaymentRow r : schedule) {
                pw.printf("%d,%.2f,%.2f,%.2f,%.2f\n", r.month, r.payment, r.principal, r.interest, r.balance);
            }
        }
    }

    // ========== CLI ==========
    public static void main(String[] args) {
        if (args.length > 0 && args[0].equals("--gui")) {
            SwingUtilities.invokeLater(() -> new LoanCalculatorGUI().setVisible(true));
            return;
        }
        // CLI
        try {
            double amount = 0, rate = 0;
            int term = 0;
            String type = "annuity";
            boolean showSchedule = false;
            String output = "";
            for (int i = 0; i < args.length; i++) {
                switch (args[i]) {
                    case "--amount": amount = Double.parseDouble(args[++i]); break;
                    case "--rate": rate = Double.parseDouble(args[++i]); break;
                    case "--term": term = Integer.parseInt(args[++i]); break;
                    case "--type": type = args[++i]; break;
                    case "--schedule": showSchedule = true; break;
                    case "--output": output = args[++i]; break;
                }
            }
            if (amount == 0 || rate == 0 || term == 0) {
                interactiveMode();
                return;
            }
            CalcResult res = calculate(amount, rate, term, type);
            System.out.printf("\nСумма кредита: %.2f\n", amount);
            System.out.printf("Ставка: %.2f%% годовых\n", rate);
            System.out.printf("Срок: %d месяцев\n", term);
            System.out.printf("Тип платежа: %s\n", type.equalsIgnoreCase("annuity") ? "Аннуитетный" : "Дифференцированный");
            System.out.printf("Общая сумма выплат: %.2f\n", res.totalPaid);
            System.out.printf("Переплата: %.2f\n", res.overpayment);
            if (showSchedule) printSchedule(res.schedule);
            if (!output.isEmpty()) {
                exportCSV(output, res.schedule);
                System.out.println("График сохранён в " + output);
            }
        } catch (Exception e) {
            System.err.println("Ошибка: " + e.getMessage());
        }
    }

    static void interactiveMode() throws Exception {
        Scanner sc = new Scanner(System.in);
        System.out.println("Интерактивный режим кредитного калькулятора");
        System.out.print("Сумма кредита: ");
        double amount = sc.nextDouble();
        System.out.print("Годовая ставка (%): ");
        double rate = sc.nextDouble();
        System.out.print("Срок (месяцев): ");
        int term = sc.nextInt();
        sc.nextLine();
        System.out.print("Тип платежа (annuity/diff): ");
        String type = sc.nextLine().trim().toLowerCase();
        if (!type.equals("diff")) type = "annuity";
        CalcResult res = calculate(amount, rate, term, type);
        System.out.printf("\nСумма кредита: %.2f\n", amount);
        System.out.printf("Ставка: %.2f%%\n", rate);
        System.out.printf("Срок: %d мес.\n", term);
        System.out.printf("Тип: %s\n", type.equals("annuity") ? "Аннуитетный" : "Дифференцированный");
        System.out.printf("Общая сумма выплат: %.2f\n", res.totalPaid);
        System.out.printf("Переплата: %.2f\n", res.overpayment);
        System.out.print("Показать график платежей? (y/n): ");
        String show = sc.nextLine().trim().toLowerCase();
        if (show.equals("y")) printSchedule(res.schedule);
        System.out.print("Сохранить график в CSV? (y/n): ");
        String save = sc.nextLine().trim().toLowerCase();
        if (save.equals("y")) {
            System.out.print("Имя файла (по умолчанию schedule.csv): ");
            String fname = sc.nextLine().trim();
            if (fname.isEmpty()) fname = "schedule.csv";
            exportCSV(fname, res.schedule);
            System.out.println("Сохранено в " + fname);
        }
    }

    // ========== GUI ==========
    static class LoanCalculatorGUI extends JFrame {
        private JTextField amountField, rateField, termField;
        private JComboBox<String> typeCombo;
        private JTextArea resultArea;
        private List<PaymentRow> currentSchedule;

        public LoanCalculatorGUI() {
            setTitle("Кредитный калькулятор");
            setSize(700, 550);
            setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
            setLayout(new BorderLayout(5,5));
            JPanel top = new JPanel(new GridBagLayout());
            GridBagConstraints gbc = new GridBagConstraints();
            gbc.insets = new Insets(5,5,5,5);
            gbc.gridx = 0; gbc.gridy = 0; top.add(new JLabel("Сумма кредита:"), gbc);
            gbc.gridx = 1; amountField = new JTextField(10); top.add(amountField, gbc);
            gbc.gridx = 2; top.add(new JLabel("Годовая ставка (%):"), gbc);
            gbc.gridx = 3; rateField = new JTextField(8); top.add(rateField, gbc);
            gbc.gridx = 4; top.add(new JLabel("Срок (мес.):"), gbc);
            gbc.gridx = 5; termField = new JTextField(6); top.add(termField, gbc);
            gbc.gridx = 6; top.add(new JLabel("Тип:"), gbc);
            gbc.gridx = 7; typeCombo = new JComboBox<>(new String[]{"annuity", "diff"}); top.add(typeCombo, gbc);
            gbc.gridx = 8; JButton calcBtn = new JButton("Рассчитать");
            calcBtn.addActionListener(e -> calculate());
            top.add(calcBtn, gbc);
            add(top, BorderLayout.NORTH);

            resultArea = new JTextArea();
            resultArea.setEditable(false);
            add(new JScrollPane(resultArea), BorderLayout.CENTER);

            JPanel bottom = new JPanel(new FlowLayout());
            JButton saveBtn = new JButton("Сохранить CSV");
            saveBtn.addActionListener(e -> saveCSV());
            bottom.add(saveBtn);
            add(bottom, BorderLayout.SOUTH);
        }

        private void calculate() {
            try {
                double amount = Double.parseDouble(amountField.getText());
                double rate = Double.parseDouble(rateField.getText());
                int term = Integer.parseInt(termField.getText());
                String type = (String) typeCombo.getSelectedItem();
                CalcResult res = LoanCalculator.calculate(amount, rate, term, type);
                currentSchedule = res.schedule;
                resultArea.setText("");
                resultArea.append(String.format("Сумма кредита: %.2f\n", amount));
                resultArea.append(String.format("Ставка: %.2f%% годовых\n", rate));
                resultArea.append(String.format("Срок: %d месяцев\n", term));
                resultArea.append(String.format("Тип: %s\n", type.equals("annuity") ? "Аннуитетный" : "Дифференцированный"));
                resultArea.append(String.format("Общая сумма выплат: %.2f\n", res.totalPaid));
                resultArea.append(String.format("Переплата: %.2f\n\n", res.overpayment));
                resultArea.append("График платежей:\n");
                resultArea.append(String.format("%-4s %-12s %-15s %-12s %-12s\n", "№", "Платёж", "Основной долг", "Проценты", "Остаток"));
                for (PaymentRow r : currentSchedule) {
                    resultArea.append(String.format("%-4d %10.2f %14.2f %11.2f %11.2f\n", r.month, r.payment, r.principal, r.interest, r.balance));
                }
            } catch (Exception ex) {
                JOptionPane.showMessageDialog(this, "Ошибка ввода: " + ex.getMessage());
            }
        }

        private void saveCSV() {
            if (currentSchedule == null || currentSchedule.isEmpty()) {
                JOptionPane.showMessageDialog(this, "Сначала выполните расчёт");
                return;
            }
            JFileChooser fc = new JFileChooser();
            if (fc.showSaveDialog(this) == JFileChooser.APPROVE_OPTION) {
                try {
                    LoanCalculator.exportCSV(fc.getSelectedFile().getAbsolutePath(), currentSchedule);
                    JOptionPane.showMessageDialog(this, "Сохранено");
                } catch (IOException ex) {
                    JOptionPane.showMessageDialog(this, "Ошибка: " + ex.getMessage());
                }
            }
        }
    }
}
