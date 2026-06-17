// loan_calc.rs - Кредитный калькулятор на Rust (CLI)
use serde::{Serialize, Deserialize};
use std::fs::File;
use std::io::{self, Write, BufRead};
use std::path::Path;
use std::str::FromStr;

#[derive(Serialize, Deserialize, Clone)]
struct PaymentRow {
    month: u32,
    payment: f64,
    principal: f64,
    interest: f64,
    balance: f64,
}

struct LoanCalculator {
    amount: f64,
    annual_rate: f64,
    term: u32,
    payment_type: String,
    monthly_rate: f64,
}

impl LoanCalculator {
    fn new(amount: f64, annual_rate: f64, term: u32, payment_type: &str) -> Self {
        LoanCalculator {
            amount,
            annual_rate,
            term,
            payment_type: payment_type.to_string(),
            monthly_rate: annual_rate / 100.0 / 12.0,
        }
    }

    fn calculate(&self) -> (Vec<PaymentRow>, f64, f64) {
        let mut schedule = Vec::new();
        let mut balance = self.amount;
        let mut total_paid = 0.0;
        let mut payment: f64;
        let mut principal: f64;
        let mut interest: f64;

        if self.payment_type == "annuity" {
            if self.monthly_rate == 0.0 {
                payment = self.amount / self.term as f64;
            } else {
                let pow = (1.0 + self.monthly_rate).powf(self.term as f64);
                payment = self.amount * self.monthly_rate * pow / (pow - 1.0);
            }
            for month in 1..=self.term {
                interest = balance * self.monthly_rate;
                principal = payment - interest;
                if principal > balance {
                    principal = balance;
                    payment = principal + interest;
                }
                balance -= principal;
                total_paid += payment;
                schedule.push(PaymentRow {
                    month,
                    payment,
                    principal,
                    interest,
                    balance,
                });
            }
        } else { // diff
            let principal_per_month = self.amount / self.term as f64;
            let mut pp = principal_per_month;
            for month in 1..=self.term {
                interest = balance * self.monthly_rate;
                payment = pp + interest;
                if payment > balance + interest {
                    payment = balance + interest;
                    pp = balance;
                }
                balance -= pp;
                total_paid += payment;
                schedule.push(PaymentRow {
                    month,
                    payment,
                    principal: pp,
                    interest,
                    balance,
                });
            }
        }
        let overpayment = total_paid - self.amount;
        (schedule, total_paid, overpayment)
    }

    fn print_schedule(&self, schedule: &[PaymentRow]) {
        println!("\nГрафик платежей:");
        println!("{:<4} {:<12} {:<15} {:<12} {:<12}", "№", "Платёж", "Основной долг", "Проценты", "Остаток");
        for r in schedule {
            println!("{:<4} {:>10.2} {:>14.2} {:>11.2} {:>11.2}", r.month, r.payment, r.principal, r.interest, r.balance);
        }
    }

    fn export_csv(&self, filepath: &str, schedule: &[PaymentRow]) -> Result<(), Box<dyn std::error::Error>> {
        let mut writer = csv::Writer::from_path(filepath)?;
        writer.write_record(&["month", "payment", "principal", "interest", "balance"])?;
        for r in schedule {
            writer.serialize(r)?;
        }
        writer.flush()?;
        Ok(())
    }
}

fn read_line(prompt: &str) -> String {
    print!("{}", prompt);
    io::stdout().flush().unwrap();
    let mut input = String::new();
    io::stdin().read_line(&mut input).unwrap();
    input.trim().to_string()
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let args: Vec<String> = std::env::args().collect();
    if args.len() < 2 {
        interactive_mode()?;
        return Ok(());
    }
    // парсинг аргументов упрощённый
    let mut amount = 0.0;
    let mut rate = 0.0;
    let mut term = 0;
    let mut ptype = "annuity".to_string();
    let mut schedule_flag = false;
    let mut output = String::new();
    let mut i = 1;
    while i < args.len() {
        match args[i].as_str() {
            "--amount" => { amount = args[i+1].parse().unwrap_or(0.0); i += 2; }
            "--rate" => { rate = args[i+1].parse().unwrap_or(0.0); i += 2; }
            "--term" => { term = args[i+1].parse().unwrap_or(0); i += 2; }
            "--type" => { ptype = args[i+1].clone(); i += 2; }
            "--schedule" => { schedule_flag = true; i += 1; }
            "--output" => { output = args[i+1].clone(); i += 2; }
            _ => { i += 1; }
        }
    }
    if amount == 0.0 || rate == 0.0 || term == 0 {
        interactive_mode()?;
        return Ok(());
    }
    let calc = LoanCalculator::new(amount, rate, term, &ptype);
    let (schedule, total, overpayment) = calc.calculate();
    println!("\nСумма кредита: {:.2}", amount);
    println!("Ставка: {:.2}% годовых", rate);
    println!("Срок: {} месяцев", term);
    println!("Тип платежа: {}", if ptype == "annuity" { "Аннуитетный" } else { "Дифференцированный" });
    println!("Общая сумма выплат: {:.2}", total);
    println!("Переплата: {:.2}", overpayment);
    if schedule_flag {
        calc.print_schedule(&schedule);
    }
    if !output.is_empty() {
        calc.export_csv(&output, &schedule)?;
        println!("График сохранён в {}", output);
    }
    Ok(())
}

fn interactive_mode() -> Result<(), Box<dyn std::error::Error>> {
    println!("Интерактивный режим кредитного калькулятора");
    let amount = read_line("Сумма кредита: ").parse::<f64>().unwrap_or(0.0);
    let rate = read_line("Годовая ставка (%): ").parse::<f64>().unwrap_or(0.0);
    let term = read_line("Срок (месяцев): ").parse::<u32>().unwrap_or(0);
    let ptype = read_line("Тип платежа (annuity/diff): ");
    let ptype = if ptype == "diff" { "diff" } else { "annuity" };
    let calc = LoanCalculator::new(amount, rate, term, ptype);
    let (schedule, total, overpayment) = calc.calculate();
    println!("\nСумма кредита: {:.2}", amount);
    println!("Ставка: {:.2}%", rate);
    println!("Срок: {} мес.", term);
    println!("Тип: {}", if ptype == "annuity" { "Аннуитетный" } else { "Дифференцированный" });
    println!("Общая сумма выплат: {:.2}", total);
    println!("Переплата: {:.2}", overpayment);
    let show = read_line("Показать график платежей? (y/n): ");
    if show.to_lowercase() == "y" {
        calc.print_schedule(&schedule);
    }
    let save = read_line("Сохранить график в CSV? (y/n): ");
    if save.to_lowercase() == "y" {
        let fname = read_line("Имя файла (по умолчанию schedule.csv): ");
        let fname = if fname.is_empty() { "schedule.csv".to_string() } else { fname };
        calc.export_csv(&fname, &schedule)?;
        println!("Сохранено в {}", fname);
    }
    Ok(())
}
