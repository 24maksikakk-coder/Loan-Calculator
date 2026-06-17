// loan_calc.go - Кредитный калькулятор на Go (CLI)
package main

import (
	"bufio"
	"encoding/csv"
	"flag"
	"fmt"
	"math"
	"os"
	"strconv"
	"strings"
)

type LoanCalculator struct {
	amount      float64
	annualRate  float64
	term        int
	paymentType string
	monthlyRate float64
}

type PaymentRow struct {
	Month     int
	Payment   float64
	Principal float64
	Interest  float64
	Balance   float64
}

func NewLoanCalculator(amount float64, rate float64, term int, ptype string) *LoanCalculator {
	return &LoanCalculator{
		amount:      amount,
		annualRate:  rate,
		term:        term,
		paymentType: ptype,
		monthlyRate: rate / 100 / 12,
	}
}

func (lc *LoanCalculator) Calculate() ([]PaymentRow, float64, float64) {
	schedule := []PaymentRow{}
	balance := lc.amount
	totalPaid := 0.0
	var payment, principal, interest float64

	if lc.paymentType == "annuity" {
		if lc.monthlyRate == 0 {
			payment = lc.amount / float64(lc.term)
		} else {
			pow := math.Pow(1+lc.monthlyRate, float64(lc.term))
			payment = lc.amount * lc.monthlyRate * pow / (pow - 1)
		}
		for month := 1; month <= lc.term; month++ {
			interest = balance * lc.monthlyRate
			principal = payment - interest
			if principal > balance {
				principal = balance
				payment = principal + interest
			}
			balance -= principal
			totalPaid += payment
			schedule = append(schedule, PaymentRow{
				Month:     month,
				Payment:   payment,
				Principal: principal,
				Interest:  interest,
				Balance:   balance,
			})
		}
	} else { // diff
		principalPerMonth := lc.amount / float64(lc.term)
		for month := 1; month <= lc.term; month++ {
			interest = balance * lc.monthlyRate
			payment = principalPerMonth + interest
			if payment > balance+interest {
				payment = balance + interest
				principalPerMonth = balance
			}
			balance -= principalPerMonth
			totalPaid += payment
			schedule = append(schedule, PaymentRow{
				Month:     month,
				Payment:   payment,
				Principal: principalPerMonth,
				Interest:  interest,
				Balance:   balance,
			})
		}
	}
	overpayment := totalPaid - lc.amount
	return schedule, totalPaid, overpayment
}

func (lc *LoanCalculator) PrintSchedule(schedule []PaymentRow) {
	fmt.Println("\nГрафик платежей:")
	fmt.Printf("%-4s %-12s %-15s %-12s %-12s\n", "№", "Платёж", "Основной долг", "Проценты", "Остаток")
	for _, r := range schedule {
		fmt.Printf("%-4d %10.2f %14.2f %11.2f %11.2f\n", r.Month, r.Payment, r.Principal, r.Interest, r.Balance)
	}
}

func (lc *LoanCalculator) ExportCSV(filepath string, schedule []PaymentRow) {
	file, err := os.Create(filepath)
	if err != nil {
		fmt.Println("Ошибка создания файла:", err)
		return
	}
	defer file.Close()
	writer := csv.NewWriter(file)
	defer writer.Flush()
	writer.Write([]string{"month", "payment", "principal", "interest", "balance"})
	for _, r := range schedule {
		writer.Write([]string{
			strconv.Itoa(r.Month),
			fmt.Sprintf("%.2f", r.Payment),
			fmt.Sprintf("%.2f", r.Principal),
			fmt.Sprintf("%.2f", r.Interest),
			fmt.Sprintf("%.2f", r.Balance),
		})
	}
}

func main() {
	var (
		amount  float64
		rate    float64
		term    int
		ptype   string
		scheduleFlag bool
		output  string
	)
	flag.Float64Var(&amount, "amount", 0, "Сумма кредита")
	flag.Float64Var(&rate, "rate", 0, "Годовая ставка (%)")
	flag.IntVar(&term, "term", 0, "Срок (месяцев)")
	flag.StringVar(&ptype, "type", "annuity", "Тип платежа (annuity/diff)")
	flag.BoolVar(&scheduleFlag, "schedule", false, "Показать график")
	flag.StringVar(&output, "output", "", "Сохранить график в CSV")
	flag.Parse()

	if amount == 0 || rate == 0 || term == 0 {
		interactiveMode()
		return
	}

	calc := NewLoanCalculator(amount, rate, term, ptype)
	schedule, total, overpayment := calc.Calculate()
	fmt.Printf("\nСумма кредита: %.2f\n", amount)
	fmt.Printf("Ставка: %.2f%% годовых\n", rate)
	fmt.Printf("Срок: %d месяцев\n", term)
	fmt.Printf("Тип платежа: %s\n", map[string]string{"annuity": "Аннуитетный", "diff": "Дифференцированный"}[ptype])
	fmt.Printf("Общая сумма выплат: %.2f\n", total)
	fmt.Printf("Переплата: %.2f\n", overpayment)
	if scheduleFlag {
		calc.PrintSchedule(schedule)
	}
	if output != "" {
		calc.ExportCSV(output, schedule)
		fmt.Printf("График сохранён в %s\n", output)
	}
}

func interactiveMode() {
	scanner := bufio.NewScanner(os.Stdin)
	fmt.Println("Интерактивный режим кредитного калькулятора")
	fmt.Print("Сумма кредита: ")
	scanner.Scan()
	amount, _ := strconv.ParseFloat(scanner.Text(), 64)
	fmt.Print("Годовая ставка (%): ")
	scanner.Scan()
	rate, _ := strconv.ParseFloat(scanner.Text(), 64)
	fmt.Print("Срок (месяцев): ")
	scanner.Scan()
	term, _ := strconv.Atoi(scanner.Text())
	fmt.Print("Тип платежа (annuity/diff): ")
	scanner.Scan()
	ptype := strings.ToLower(scanner.Text())
	if ptype != "annuity" && ptype != "diff" {
		ptype = "annuity"
	}
	calc := NewLoanCalculator(amount, rate, term, ptype)
	schedule, total, overpayment := calc.Calculate()
	fmt.Printf("\nСумма кредита: %.2f\n", amount)
	fmt.Printf("Ставка: %.2f%%\n", rate)
	fmt.Printf("Срок: %d мес.\n", term)
	fmt.Printf("Тип: %s\n", map[string]string{"annuity": "Аннуитетный", "diff": "Дифференцированный"}[ptype])
	fmt.Printf("Общая сумма выплат: %.2f\n", total)
	fmt.Printf("Переплата: %.2f\n", overpayment)
	fmt.Print("Показать график платежей? (y/n): ")
	scanner.Scan()
	if strings.ToLower(scanner.Text()) == "y" {
		calc.PrintSchedule(schedule)
	}
	fmt.Print("Сохранить график в CSV? (y/n): ")
	scanner.Scan()
	if strings.ToLower(scanner.Text()) == "y" {
		fmt.Print("Имя файла (по умолчанию schedule.csv): ")
		scanner.Scan()
		fname := scanner.Text()
		if fname == "" {
			fname = "schedule.csv"
		}
		calc.ExportCSV(fname, schedule)
		fmt.Printf("Сохранено в %s\n", fname)
	}
}
