<?php
// loan_calc.php - Кредитный калькулятор на PHP (CLI + веб)
// CLI: php loan_calc.php --amount=1000000 --rate=12 --term=60 --type=annuity --schedule

function calculateLoan($amount, $rate, $term, $type) {
    $schedule = [];
    $balance = $amount;
    $totalPaid = 0;
    $monthlyRate = $rate / 100 / 12;

    if ($type == 'annuity') {
        if ($monthlyRate == 0) {
            $payment = $amount / $term;
        } else {
            $pow = pow(1 + $monthlyRate, $term);
            $payment = $amount * $monthlyRate * $pow / ($pow - 1);
        }
        for ($month = 1; $month <= $term; $month++) {
            $interest = $balance * $monthlyRate;
            $principal = $payment - $interest;
            if ($principal > $balance) { $principal = $balance; $payment = $principal + $interest; }
            $balance -= $principal;
            $totalPaid += $payment;
            $schedule[] = ['month' => $month, 'payment' => $payment, 'principal' => $principal, 'interest' => $interest, 'balance' => $balance];
        }
    } else { // diff
        $principalPerMonth = $amount / $term;
        for ($month = 1; $month <= $term; $month++) {
            $interest = $balance * $monthlyRate;
            $payment = $principalPerMonth + $interest;
            if ($payment > $balance + $interest) { $payment = $balance + $interest; $principalPerMonth = $balance; }
            $balance -= $principalPerMonth;
            $totalPaid += $payment;
            $schedule[] = ['month' => $month, 'payment' => $payment, 'principal' => $principalPerMonth, 'interest' => $interest, 'balance' => $balance];
        }
    }
    $overpayment = $totalPaid - $amount;
    return [$schedule, $totalPaid, $overpayment];
}

function printSchedule($schedule) {
    echo "\nГрафик платежей:\n";
    printf("%-4s %-12s %-15s %-12s %-12s\n", "№", "Платёж", "Основной долг", "Проценты", "Остаток");
    foreach ($schedule as $r) {
        printf("%-4d %10.2f %14.2f %11.2f %11.2f\n", $r['month'], $r['payment'], $r['principal'], $r['interest'], $r['balance']);
    }
}

function exportCSV($file, $schedule) {
    $f = fopen($file, 'w');
    fputcsv($f, ['month', 'payment', 'principal', 'interest', 'balance']);
    foreach ($schedule as $r) {
        fputcsv($f, [$r['month'], $r['payment'], $r['principal'], $r['interest'], $r['balance']]);
    }
    fclose($f);
}

// ========== CLI ==========
if (php_sapi_name() === 'cli') {
    $options = getopt("", ["amount:", "rate:", "term:", "type:", "schedule", "output:"]);
    $amount = isset($options['amount']) ? (float)$options['amount'] : 0;
    $rate = isset($options['rate']) ? (float)$options['rate'] : 0;
    $term = isset($options['term']) ? (int)$options['term'] : 0;
    $type = isset($options['type']) ? $options['type'] : 'annuity';
    $showSchedule = isset($options['schedule']);
    $output = $options['output'] ?? '';

    if ($amount == 0 || $rate == 0 || $term == 0) {
        // interactive mode
        echo "Интерактивный режим кредитного калькулятора\n";
        echo "Сумма кредита: ";
        $amount = (float)trim(fgets(STDIN));
        echo "Годовая ставка (%): ";
        $rate = (float)trim(fgets(STDIN));
        echo "Срок (месяцев): ";
        $term = (int)trim(fgets(STDIN));
        echo "Тип платежа (annuity/diff): ";
        $type = trim(fgets(STDIN));
        if (!in_array($type, ['annuity','diff'])) $type = 'annuity';
        list($schedule, $total, $overpayment) = calculateLoan($amount, $rate, $term, $type);
        echo "\nСумма кредита: " . number_format($amount, 2) . "\n";
        echo "Ставка: " . number_format($rate, 2) . "%\n";
        echo "Срок: $term мес.\n";
        echo "Тип: " . ($type == 'annuity' ? 'Аннуитетный' : 'Дифференцированный') . "\n";
        echo "Общая сумма выплат: " . number_format($total, 2) . "\n";
        echo "Переплата: " . number_format($overpayment, 2) . "\n";
        echo "Показать график платежей? (y/n): ";
        if (trim(fgets(STDIN)) == 'y') printSchedule($schedule);
        echo "Сохранить график в CSV? (y/n): ";
        if (trim(fgets(STDIN)) == 'y') {
            echo "Имя файла (по умолчанию schedule.csv): ";
            $fname = trim(fgets(STDIN));
            if (empty($fname)) $fname = 'schedule.csv';
            exportCSV($fname, $schedule);
            echo "Сохранено в $fname\n";
        }
        exit;
    }

    list($schedule, $total, $overpayment) = calculateLoan($amount, $rate, $term, $type);
    echo "\nСумма кредита: " . number_format($amount, 2) . "\n";
    echo "Ставка: " . number_format($rate, 2) . "% годовых\n";
    echo "Срок: $term месяцев\n";
    echo "Тип платежа: " . ($type == 'annuity' ? 'Аннуитетный' : 'Дифференцированный') . "\n";
    echo "Общая сумма выплат: " . number_format($total, 2) . "\n";
    echo "Переплата: " . number_format($overpayment, 2) . "\n";
    if ($showSchedule) printSchedule($schedule);
    if ($output) {
        exportCSV($output, $schedule);
        echo "График сохранён в $output\n";
    }
    exit;
}

// ========== ВЕБ-ИНТЕРФЕЙС ==========
?>
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Кредитный калькулятор (PHP)</title>
    <style>
        body { font-family: 'Segoe UI', sans-serif; background: #f4f7fb; margin: 20px; }
        .container { max-width: 800px; margin: 0 auto; background: white; padding: 20px; border-radius: 16px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .form-row { margin: 10px 0; }
        .form-row label { display: inline-block; width: 150px; }
        input, select, button { padding: 6px; margin: 2px; }
        button { background: #3498db; color: white; border: none; border-radius: 4px; cursor: pointer; }
        table { width: 100%; border-collapse: collapse; margin-top: 20px; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: right; }
        th { background: #2c3e50; color: white; }
        .result { background: #e8f5e9; padding: 10px; border-radius: 8px; margin-top: 10px; }
    </style>
</head>
<body>
<div class="container">
    <h1>💰 Кредитный калькулятор</h1>
    <form method="GET">
        <div class="form-row">
            <label>Сумма кредита:</label>
            <input type="number" step="any" name="amount" value="<?= $_GET['amount'] ?? '' ?>" required>
        </div>
        <div class="form-row">
            <label>Годовая ставка (%):</label>
            <input type="number" step="any" name="rate" value="<?= $_GET['rate'] ?? '' ?>" required>
        </div>
        <div class="form-row">
            <label>Срок (месяцев):</label>
            <input type="number" name="term" value="<?= $_GET['term'] ?? '' ?>" required>
        </div>
        <div class="form-row">
            <label>Тип платежа:</label>
            <select name="type">
                <option value="annuity" <?= isset($_GET['type']) && $_GET['type']=='annuity' ? 'selected' : '' ?>>Аннуитетный</option>
                <option value="diff" <?= isset($_GET['type']) && $_GET['type']=='diff' ? 'selected' : '' ?>>Дифференцированный</option>
            </select>
        </div>
        <button type="submit">Рассчитать</button>
        <a href="?">Сбросить</a>
    </form>

    <?php if (isset($_GET['amount']) && isset($_GET['rate']) && isset($_GET['term'])): 
        $amount = (float)$_GET['amount'];
        $rate = (float)$_GET['rate'];
        $term = (int)$_GET['term'];
        $type = $_GET['type'] ?? 'annuity';
        list($schedule, $total, $overpayment) = calculateLoan($amount, $rate, $term, $type);
    ?>
        <div class="result">
            <p><strong>Сумма кредита:</strong> <?= number_format($amount, 2) ?></p>
            <p><strong>Ставка:</strong> <?= number_format($rate, 2) ?>% годовых</p>
            <p><strong>Срок:</strong> <?= $term ?> месяцев</p>
            <p><strong>Тип:</strong> <?= $type == 'annuity' ? 'Аннуитетный' : 'Дифференцированный' ?></p>
            <p><strong>Общая сумма выплат:</strong> <?= number_format($total, 2) ?></p>
            <p><strong>Переплата:</strong> <?= number_format($overpayment, 2) ?></p>
        </div>
        <h3>График платежей</h3>
        <table>
            <tr><th>№</th><th>Платёж</th><th>Основной долг</th><th>Проценты</th><th>Остаток</th></tr>
            <?php foreach ($schedule as $r): ?>
                <tr>
                    <td><?= $r['month'] ?></td>
                    <td><?= number_format($r['payment'], 2) ?></td>
                    <td><?= number_format($r['principal'], 2) ?></td>
                    <td><?= number_format($r['interest'], 2) ?></td>
                    <td><?= number_format($r['balance'], 2) ?></td>
                </tr>
            <?php endforeach; ?>
        </table>
        <p><a href="?export=1&amount=<?= $amount ?>&rate=<?= $rate ?>&term=<?= $term ?>&type=<?= $type ?>">📤 Скачать CSV</a></p>
    <?php endif; ?>

    <?php
    if (isset($_GET['export']) && isset($_GET['amount']) && isset($_GET['rate']) && isset($_GET['term'])) {
        $amount = (float)$_GET['amount'];
        $rate = (float)$_GET['rate'];
        $term = (int)$_GET['term'];
        $type = $_GET['type'] ?? 'annuity';
        list($schedule, $total, $overpayment) = calculateLoan($amount, $rate, $term, $type);
        header('Content-Type: text/csv');
        header('Content-Disposition: attachment; filename="schedule.csv"');
        $f = fopen('php://output', 'w');
        fputcsv($f, ['month','payment','principal','interest','balance']);
        foreach ($schedule as $r) fputcsv($f, [$r['month'], $r['payment'], $r['principal'], $r['interest'], $r['balance']]);
        fclose($f);
        exit;
    }
    ?>
</div>
</body>
</html>
