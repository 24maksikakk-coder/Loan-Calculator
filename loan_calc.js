#!/usr/bin/env node
/**
 * loan_calc.js - Кредитный калькулятор на JavaScript (Node.js CLI)
 */
const fs = require('fs');
const { program } = require('commander');

class LoanCalculator {
    constructor(amount, annualRate, termMonths, paymentType) {
        this.amount = amount;
        this.annualRate = annualRate;
        this.term = termMonths;
        this.paymentType = paymentType;
        this.monthlyRate = annualRate / 100 / 12;
    }

    calculate() {
        const schedule = [];
        let balance = this.amount;
        let totalPaid = 0;
        let payment, principal, interest;

        if (this.paymentType === 'annuity') {
            if (this.monthlyRate === 0) {
                payment = this.amount / this.term;
            } else {
                const pow = Math.pow(1 + this.monthlyRate, this.term);
                payment = this.amount * this.monthlyRate * pow / (pow - 1);
            }
            for (let month = 1; month <= this.term; month++) {
                interest = balance * this.monthlyRate;
                principal = payment - interest;
                if (principal > balance) principal = balance;
                balance -= principal;
                totalPaid += payment;
                schedule.push({ month, payment, principal, interest, balance });
            }
        } else { // diff
            const principalPerMonth = this.amount / this.term;
            for (let month = 1; month <= this.term; month++) {
                interest = balance * this.monthlyRate;
                payment = principalPerMonth + interest;
                if (payment > balance + interest) {
                    payment = balance + interest;
                    principalPerMonth = balance;
                }
                balance -= principalPerMonth;
                totalPaid += payment;
                schedule.push({ month, payment, principal: principalPerMonth, interest, balance });
            }
        }
        const overpayment = totalPaid - this.amount;
        return { schedule, totalPaid, overpayment };
    }

    exportCSV(filepath, schedule) {
        const header = 'month,payment,principal,interest,balance\n';
        const rows = schedule.map(r => `${r.month},${r.payment.toFixed(2)},${r.principal.toFixed(2)},${r.interest.toFixed(2)},${r.balance.toFixed(2)}`).join('\n');
        fs.writeFileSync(filepath, header + rows);
    }

    printSchedule(schedule) {
        console.log('\nГрафик платежей:');
        console.log('№    Платёж     Основной долг  Проценты   Остаток');
        schedule.forEach(r => {
            console.log(`${r.month.toString().padEnd(4)} ${r.payment.toFixed(2).padStart(10)} ${r.principal.toFixed(2).padStart(14)} ${r.interest.toFixed(2).padStart(10)} ${r.balance.toFixed(2).padStart(10)}`);
        });
    }
}

program
    .option('-a, --amount <amount>', 'Сумма кредита', parseFloat)
    .option('-r, --rate <rate>', 'Годовая ставка (%)', parseFloat)
    .option('-t, --term <term>', 'Срок (месяцев)', parseInt)
    .option('--type <type>', 'Тип платежа (annuity/diff)', 'annuity')
    .option('--schedule', 'Показать график')
    .option('-o, --output <file>', 'Сохранить график в CSV')
    .parse(process.argv);

const opts = program.opts();

if (!opts.amount || !opts.rate || !opts.term) {
    // интерактивный режим
    const readline = require('readline');
    const rl = readline.createInterface({ input: process.stdin, output: process.stdout });
    const prompt = (q) => new Promise(resolve => rl.question(q, resolve));
    (async () => {
        console.log('Интерактивный режим кредитного калькулятора');
        try {
            const amount = parseFloat(await prompt('Сумма кредита: '));
            const rate = parseFloat(await prompt('Годовая ставка (%): '));
            const term = parseInt(await prompt('Срок (месяцев): '));
            const type = (await prompt('Тип платежа (annuity/diff): ')).toLowerCase() || 'annuity';
            const calc = new LoanCalculator(amount, rate, term, type);
            const { schedule, totalPaid, overpayment } = calc.calculate();
            console.log(`\nСумма кредита: ${amount.toFixed(2)}`);
            console.log(`Ставка: ${rate.toFixed(2)}%`);
            console.log(`Срок: ${term} мес.`);
            console.log(`Тип: ${type === 'annuity' ? 'Аннуитетный' : 'Дифференцированный'}`);
            console.log(`Общая сумма выплат: ${totalPaid.toFixed(2)}`);
            console.log(`Переплата: ${overpayment.toFixed(2)}`);
            const show = await prompt('Показать график платежей? (y/n): ');
            if (show.toLowerCase() === 'y') calc.printSchedule(schedule);
            const save = await prompt('Сохранить график в CSV? (y/n): ');
            if (save.toLowerCase() === 'y') {
                const fname = await prompt('Имя файла (по умолчанию schedule.csv): ') || 'schedule.csv';
                calc.exportCSV(fname, schedule);
                console.log(`Сохранено в ${fname}`);
            }
        } catch (e) { console.error(e.message); }
        rl.close();
    })();
} else {
    const calc = new LoanCalculator(opts.amount, opts.rate, opts.term, opts.type);
    const { schedule, totalPaid, overpayment } = calc.calculate();
    console.log(`\nСумма кредита: ${opts.amount.toFixed(2)}`);
    console.log(`Ставка: ${opts.rate.toFixed(2)}%`);
    console.log(`Срок: ${opts.term} мес.`);
    console.log(`Тип: ${opts.type === 'annuity' ? 'Аннуитетный' : 'Дифференцированный'}`);
    console.log(`Общая сумма выплат: ${totalPaid.toFixed(2)}`);
    console.log(`Переплата: ${overpayment.toFixed(2)}`);
    if (opts.schedule) calc.printSchedule(schedule);
    if (opts.output) {
        calc.exportCSV(opts.output, schedule);
        console.log(`График сохранён в ${opts.output}`);
    }
}
