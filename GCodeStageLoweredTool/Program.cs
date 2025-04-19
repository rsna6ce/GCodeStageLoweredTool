using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

namespace GCodeStageLoweredTool
{
    class Program
    {
        [STAThread] // MessageBox用
        static void Main(string[] args)
        {

            // コマンドライン引数のチェック
            if (args.Length == 0)
            {
                MessageBox.Show("Gコードファイルを指定してください（例: 右クリック > 送る）。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string inputFilePath = args[0];
            if (!File.Exists(inputFilePath))
            {
                MessageBox.Show($"ファイルが見つかりません: {inputFilePath}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Z値解析と適用済みチェック
                double maxZ = 0;
                bool isAlreadyApplied = false;
                Regex re = new Regex(@"Z([0-9]+(\.[0-9]+)?)", RegexOptions.IgnoreCase);
                foreach (var line in File.ReadAllLines(inputFilePath))
                {
                    if (line.Contains("; StageLoweredBy70mm"))
                    {
                        isAlreadyApplied = true;
                        break;
                    }
                    if (line.Contains("G1") || line.Contains("G0"))
                    {
                        var match = re.Match(line);
                        if (match.Success && double.TryParse(match.Groups[1].Value, out double zValue))
                        {
                            if (zValue > maxZ) maxZ = zValue;
                        }
                    }
                }

                if (isAlreadyApplied)
                {
                    MessageBox.Show("このGコードはすでに適用済みです（ステージ下げコマンドが含まれています）。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Gコード改変
                var lines = new List<string>(File.ReadAllLines(inputFilePath));
                if (maxZ <= 100)
                {
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].Contains("M300"))
                        {
                            lines[i] = $"; {lines[i]} ; Commented out original M300";
                        }
                    }
                    lines.Add("G1 Z70 F600 ; Lower stage by 70mm");
                    lines.Add("M300 P300 S4000 ; Beep after lowering");
                    lines.Add("; StageLoweredBy70mm");
                }

                // ファイル上書き
                File.WriteAllLines(inputFilePath, lines);
                MessageBox.Show($"処理完了！最大高さ: {maxZ}mm", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラーが発生しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
    }
}
