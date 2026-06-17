#!/usr/bin/env python3
"""
loan_calc.py - Кредитный калькулятор на Python (CLI + Tkinter GUI)
Поддерживает: аннуитетный и дифференцированный платежи, график, экспорт CSV.
"""
import argparse
import sys
import csv
import os
from math import pow
from datetime import datetime
from typing import List, Tuple, Dict

try:
    import tkinter as tk
    from tkinter import ttk, filedialog, messagebox, scrolledtext
    GUI_AVAILABLE = True
except ImportError:
    GUI_AVAILABLE = False

class LoanCalculator:
    def __init__(self, amount: float, annual_rate: float, term_months: int, payment_type: str):
        self.amount = amount
        self.annual_rate = annual_rate
        self.term = term_months
        self.payment_type = payment_type.lower()  # 'annuity' or 'diff'
        self.monthly_rate = annual_rate / 100 / 12

    def calculate(self) -> Tuple[List[Dict], float, float]:
        """
        Возвращает (график_платежей, общая_сумма_выплат, переплата)
        График: список словарей с ключами: month, payment, principal, interest, balance
        """
        schedule = []
        balance = self.amount
        total_paid = 0.0
        if self.payment_type == 'annuity':
            if self.monthly_rate == 0:
                payment = self.amount / self.term
            else:
                payment = self.amount * self.monthly_rate * pow(1 + self.monthly_rate, self.term) / (pow(1 + self.monthly_rate, self.term) - 1)
            for month in range(1, self.term + 1):
                interest = balance * self.monthly_rate
                principal = payment - interest
                if principal > balance:
                    principal = balance
                    payment = principal + interest
                balance -= principal
                total_paid += payment
                schedule.append({
                    'month': month,
                    'payment': payment,
                    'principal': principal,
                    'interest': interest,
                    'balance': balance
                })
        else:  # дифференцированный
            principal_per_month = self.amount / self.term
            for month in range(1, self.term + 1):
                interest = balance * self.monthly_rate
                payment = principal_per_month + interest
                if payment > balance + interest:
                    payment = balance + interest
                    principal_per_month = balance
                balance -= principal_per_month
                total_paid += payment
                schedule.append({
                    'month': month,
                    'payment': payment,
                    'principal': principal_per_month,
                    'interest': interest,
                    'balance': balance
                })
        overpayment = total_paid - self.amount
        return schedule, total_paid, overpayment

    def export_csv(self, filepath: str, schedule: List[Dict]):
        with open(filepath, 'w', newline='', encoding='utf-8') as f:
            writer = csv.DictWriter(f, fieldnames=['month', 'payment', 'principal', 'interest', 'balance'])
            writer.writeheader()
            writer.writerows(schedule)

    def print_schedule(self, schedule: List[Dict]):
        print("\nГрафик платежей:")
        print(f"{'№':<4} {'Платёж':<12} {'Основной долг':<15} {'Проценты':<12} {'Остаток':<12}")
        for row in schedule:
            print(f"{row['month']:<4} {row['payment']:>10.2f} {row['principal']:>14.2f} {row['interest']:>11.2f} {row['balance']:>11.2f}")

# ========== CLI ==========
def cli():
    parser = argparse.ArgumentParser(description="Кредитный калькулятор")
    parser.add_argument("--amount", type=float, help="Сумма кредита")
    parser.add_argument("--rate", type=float, help="Годовая ставка (%)")
    parser.add_argument("--term", type=int, help="Срок в месяцах")
    parser.add_argument("--type", choices=['annuity','diff'], default='annuity', help="Тип платежа")
    parser.add_argument("--schedule", action="store_true", help="Показать график платежей")
    parser.add_argument("--output", help="Сохранить график в CSV")
    parser.add_argument("--gui", action="store_true", help="Запустить GUI")
    args = parser.parse_args()

    if args.gui and GUI_AVAILABLE:
        root = tk.Tk()
        app = LoanCalculatorGUI(root)
        root.mainloop()
        return

    if args.amount is None or args.rate is None or args.term is None:
        interactive_mode()
        return

    calc = LoanCalculator(args.amount, args.rate, args.term, args.type)
    schedule, total, overpayment = calc.calculate()
    print(f"\nСумма кредита: {args.amount:.2f}")
    print(f"Ставка: {args.rate:.2f}% годовых")
    print(f"Срок: {args.term} месяцев")
    print(f"Тип платежа: {'Аннуитетный' if args.type == 'annuity' else 'Дифференцированный'}")
    print(f"Общая сумма выплат: {total:.2f}")
    print(f"Переплата: {overpayment:.2f}")
    if args.schedule:
        calc.print_schedule(schedule)
    if args.output:
        calc.export_csv(args.output, schedule)
        print(f"График сохранён в {args.output}")

def interactive_mode():
    print("Интерактивный режим кредитного калькулятора")
    try:
        amount = float(input("Сумма кредита: "))
        rate = float(input("Годовая ставка (%): "))
        term = int(input("Срок (месяцев): "))
        ptype = input("Тип платежа (annuity/diff): ").strip().lower()
        if ptype not in ['annuity','diff']:
            ptype = 'annuity'
        calc = LoanCalculator(amount, rate, term, ptype)
        schedule, total, overpayment = calc.calculate()
        print(f"\nСумма кредита: {amount:.2f}")
        print(f"Ставка: {rate:.2f}%")
        print(f"Срок: {term} мес.")
        print(f"Тип: {'Аннуитетный' if ptype == 'annuity' else 'Дифференцированный'}")
        print(f"Общая сумма выплат: {total:.2f}")
        print(f"Переплата: {overpayment:.2f}")
        show = input("Показать график платежей? (y/n): ").strip().lower()
        if show == 'y':
            calc.print_schedule(schedule)
        save = input("Сохранить график в CSV? (y/n): ").strip().lower()
        if save == 'y':
            fname = input("Имя файла (по умолчанию schedule.csv): ").strip()
            if not fname:
                fname = "schedule.csv"
            calc.export_csv(fname, schedule)
            print(f"Сохранено в {fname}")
    except Exception as e:
        print(f"Ошибка: {e}")

# ========== GUI ==========
if GUI_AVAILABLE:
    class LoanCalculatorGUI:
        def __init__(self, root):
            self.root = root
            self.root.title("Кредитный калькулятор")
            self.root.geometry("700x600")
            self.root.resizable(True, True)
            self.create_widgets()

        def create_widgets(self):
            main = ttk.Frame(self.root, padding="10")
            main.pack(fill=tk.BOTH, expand=True)

            # Поля ввода
            row = 0
            ttk.Label(main, text="Сумма кредита:").grid(row=row, column=0, sticky="w", pady=5)
            self.amount_var = tk.StringVar()
            ttk.Entry(main, textvariable=self.amount_var).grid(row=row, column=1, padx=5, pady=5)
            row += 1
            ttk.Label(main, text="Годовая ставка (%):").grid(row=row, column=0, sticky="w", pady=5)
            self.rate_var = tk.StringVar()
            ttk.Entry(main, textvariable=self.rate_var).grid(row=row, column=1, padx=5, pady=5)
            row += 1
            ttk.Label(main, text="Срок (месяцев):").grid(row=row, column=0, sticky="w", pady=5)
            self.term_var = tk.StringVar()
            ttk.Entry(main, textvariable=self.term_var).grid(row=row, column=1, padx=5, pady=5)
            row += 1
            ttk.Label(main, text="Тип платежа:").grid(row=row, column=0, sticky="w", pady=5)
            self.type_var = tk.StringVar(value="annuity")
            ttk.Combobox(main, textvariable=self.type_var, values=["annuity", "diff"], state="readonly").grid(row=row, column=1, padx=5, pady=5)
            row += 1
            ttk.Button(main, text="Рассчитать", command=self.calculate).grid(row=row, column=0, columnspan=2, pady=10)
            row += 1
            self.result_text = scrolledtext.ScrolledText(main, height=15, width=70)
            self.result_text.grid(row=row, column=0, columnspan=2, pady=5)
            row += 1
            btn_frame = ttk.Frame(main)
            btn_frame.grid(row=row, column=0, columnspan=2, pady=5)
            ttk.Button(btn_frame, text="Сохранить CSV", command=self.save_csv).pack(side=tk.LEFT, padx=5)
            ttk.Button(btn_frame, text="Очистить", command=self.clear).pack(side=tk.LEFT, padx=5)

        def calculate(self):
            try:
                amount = float(self.amount_var.get())
                rate = float(self.rate_var.get())
                term = int(self.term_var.get())
                ptype = self.type_var.get()
                calc = LoanCalculator(amount, rate, term, ptype)
                schedule, total, overpayment = calc.calculate()
                self.result_text.delete(1.0, tk.END)
                self.result_text.insert(tk.END, f"Сумма кредита: {amount:.2f}\n")
                self.result_text.insert(tk.END, f"Ставка: {rate:.2f}% годовых\n")
                self.result_text.insert(tk.END, f"Срок: {term} месяцев\n")
                self.result_text.insert(tk.END, f"Тип: {'Аннуитетный' if ptype == 'annuity' else 'Дифференцированный'}\n")
                self.result_text.insert(tk.END, f"Общая сумма выплат: {total:.2f}\n")
                self.result_text.insert(tk.END, f"Переплата: {overpayment:.2f}\n\n")
                self.result_text.insert(tk.END, "График платежей:\n")
                self.result_text.insert(tk.END, f"{'№':<4} {'Платёж':<12} {'Основной долг':<15} {'Проценты':<12} {'Остаток':<12}\n")
                for row in schedule:
                    self.result_text.insert(tk.END,
                        f"{row['month']:<4} {row['payment']:>10.2f} {row['principal']:>14.2f} {row['interest']:>11.2f} {row['balance']:>11.2f}\n")
                self._schedule = schedule
            except Exception as e:
                messagebox.showerror("Ошибка", str(e))

        def save_csv(self):
            if hasattr(self, '_schedule'):
                filepath = filedialog.asksaveasfilename(defaultextension=".csv", filetypes=[("CSV", "*.csv")])
                if filepath:
                    calc = LoanCalculator(0,0,0,'annuity')  # dummy
                    calc.export_csv(filepath, self._schedule)
                    messagebox.showinfo("Сохранено", f"Сохранено в {filepath}")
            else:
                messagebox.showwarning("Нет данных", "Сначала выполните расчёт")

        def clear(self):
            self.amount_var.set("")
            self.rate_var.set("")
            self.term_var.set("")
            self.result_text.delete(1.0, tk.END)

if __name__ == "__main__":
    cli()
