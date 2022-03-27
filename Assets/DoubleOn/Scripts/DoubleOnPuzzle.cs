using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleOnPuzzle {
	public const int WIDTH = 5;
	public const int HEIGHT = 7;
	// public const int WIDTH = 10;
	// public const int HEIGHT = 14;
	public const int COLORS_COUNT = 6;

	private static readonly int[] DX = new[] { 1, 0, -1, 0 };
	private static readonly int[] DY = new[] { 0, -1, 0, 1 };
	private static readonly Vector2Int[] DD = Enumerable.Range(0, 4).Select(i => new Vector2Int(DX[i], DY[i])).ToArray();

	public Vector2Int[] LEDPositions;
	public Vector2Int[] ButtonPositions;
	public int[][] ButtonColors;
	public int[] ColorCounts = new int[COLORS_COUNT];
	public int[] Solution;
	public int[] Presses;
	public int[] LeftColorPresses;
	public bool[] LitLEDs;
	public List<int> SolutionLog;

	public DoubleOnPuzzle() {
		bool[][] usedCells = Enumerable.Range(0, WIDTH).Select(_ => Enumerable.Range(0, HEIGHT).Select(_1 => false).ToArray()).ToArray();
		List<Vector2Int> ledPositions = new List<Vector2Int>();
		List<Vector2Int> buttonPositions = new List<Vector2Int>();
		Vector2Int[] positionQ = Enumerable.Range(0, WIDTH).SelectMany(x => Enumerable.Range(0, HEIGHT).Select(y => new Vector2Int(x, y))).ToArray();
		positionQ.Shuffle();
		foreach (Vector2Int ledPosition in positionQ) {
			if (usedCells[ledPosition.x][ledPosition.y]) continue;
			List<Vector2Int> freeAdjacentCells = new List<Vector2Int>();
			foreach (Vector2Int buttonPosition in DD.Select(dd => dd + ledPosition)) {
				if (buttonPosition.x < 0 || buttonPosition.x >= WIDTH || buttonPosition.y < 0 || buttonPosition.y >= HEIGHT) continue;
				if (!usedCells[buttonPosition.x][buttonPosition.y]) freeAdjacentCells.Add(buttonPosition);
			}
			if (freeAdjacentCells.Count < 2) continue;
			ledPositions.Add(ledPosition);
			freeAdjacentCells.Shuffle();
			Vector2Int[] ledButtonPositions = freeAdjacentCells.Take(2).ToArray();
			buttonPositions.AddRange(ledButtonPositions);
			usedCells[ledPosition.x][ledPosition.y] = true;
			foreach (Vector2Int pos in ledButtonPositions) usedCells[pos.x][pos.y] = true;
		}
		LEDPositions = ledPositions.ToArray();
		ButtonPositions = buttonPositions.ToArray();
		SortLEDs();
		// for (int j = 0; j < ButtonPositions.Length; j++) {
		// 	int cl1 = Random.Range(0, 6);
		// 	int cl2 = Random.Range(0, 6);
		// 	ButtonColors[j] = new[] { cl1, cl2 };
		// }
		// for (int j = 0; j < LEDPositions.Length; j++) {
		// 	foreach (int cl in ButtonColors[2 * j + Random.Range(0, 2)]) ColorCounts[cl] += 1;
		// }
		GenerateColors();
		Reset();
	}

	public void GenerateColors() {
		Solution = LEDPositions.Select(_ => Random.Range(0, 2)).ToArray();
		ButtonColors = ButtonPositions.Select(_ => new[] { -1, -1 }).ToArray();
		int[] solutionPath = GenerateSolutionPath();
		List<int>[] ledGroups = Enumerable.Range(0, COLORS_COUNT - 1).Select(_ => new List<int>()).ToArray();
		for (int j = 0; j < solutionPath.Length; j++) ledGroups[solutionPath[j]].Add(j);
		SolutionLog = new List<int>();
		for (int j = 0; j < COLORS_COUNT - 1; j++) {
			int[] leds = ledGroups[j].ToArray();
			SolutionLog.AddRange(leds);
			int[] prevLeds = j == 0 ? null : ledGroups[j - 1].ToArray();
			if (leds.Length == 0) continue;
			List<int> rndVars = new List<int>();
			if (j > 0 && prevLeds.Any(ledInd => ButtonColors[2 * ledInd + Solution[ledInd]].Any(c => c == j))) rndVars.Add(0);
			if (j == 0 || prevLeds.Any(ledInd => Enumerable.Range(0, 2).Any(bInd => ButtonColors[2 * ledInd + bInd].Count(c => c == j) == 1))) rndVars.Add(1);
			rndVars.Add(2);
			if (j == 0 || prevLeds.Any(ledInd => ButtonColors[2 * ledInd + Not(Solution[ledInd])].Any(c => c == j))) rndVars.Add(3);
			int rnd = rndVars.PickRandom();
			if (rnd == 0) {
				foreach (int ledInd in leds) ButtonColors[2 * ledInd + Not(Solution[ledInd])] = PickFrom(new[] { j, j }, new[] { -1, j }, new[] { j, -1 });
			} else if (rnd == 1) {
				ColorCounts[j] += 1;
				int posLedInd = leds.PickRandom();
				foreach (int ledInd in leds) {
					if (ledInd == posLedInd) ButtonColors[2 * ledInd + Solution[ledInd]] = PickFrom(new[] { j, -1 }, new[] { -1, j });
					else ButtonColors[2 * ledInd + Not(Solution[ledInd])] = new[] { j, j };
				}
			} else if (rnd == 3) {
				foreach (int ledInd in leds) {
					int num = Random.Range(0, 2);
					ButtonColors[2 * ledInd + Solution[ledInd]] = num == 0 ? PickFrom(new[] { j, -1 }, new[] { -1, j }) : new[] { j, j };
					ColorCounts[j] += num + 1;
				}
			} else if (leds.Length == 1) {
				ColorCounts[j] += 2;
				ButtonColors[2 * leds[0] + Solution[leds[0]]] = new[] { j, j };
			} else {
				ColorCounts[j] += 2 * (leds.Length - 1);
				int negLedInd = leds.PickRandom();
				foreach (int ledInd in leds) {
					if (ledInd == negLedInd) ButtonColors[2 * ledInd + Not(Solution[ledInd])] = PickFrom(new[] { j, -1 }, new[] { -1, j });
					else ButtonColors[2 * ledInd + Solution[ledInd]] = new[] { j, j };
				}
			}
			int nextLed = leds.PickRandom();
			int qs = Enumerable.Range(0, 4).Where(k => ButtonColors[2 * nextLed + k % 2][k / 2] == -1).PickRandom();
			ButtonColors[2 * nextLed + qs % 2][qs / 2] = j + 1;
			if (qs % 2 == Solution[nextLed]) ColorCounts[j + 1] += 1;
			foreach (int ledInd in leds) {
				if (j == 0) continue;
				if (Random.Range(0, 2) == 0) continue;
				if (Enumerable.Range(0, 2).Select(k => ButtonColors[2 * ledInd + k].Any(v => v < 0)).Any(t => !t)) continue;
				int colInd = Random.Range(0, j);
				for (int k = 0; k < 2; k++) {
					List<int> pos = Enumerable.Range(0, 2).Where(c => ButtonColors[2 * ledInd + k][c] < 0).ToList();
					ButtonColors[2 * ledInd + k][pos.PickRandom()] = colInd;
				}
				ColorCounts[colInd] += 1;
			}
			foreach (int ledInd in leds) {
				for (int bInd = 0; bInd < 2; bInd++) {
					for (int cInd = 0; cInd < 2; cInd++) {
						if (ButtonColors[2 * ledInd + bInd][cInd] >= 0) continue;
						int col = Random.Range(j + 1, COLORS_COUNT);
						ButtonColors[2 * ledInd + bInd][cInd] = col;
						if (Solution[ledInd] == bInd) ColorCounts[col] += 1;
					}
				}
			}
		}
	}

	public void Reset() {
		LitLEDs = new bool[LEDPositions.Length];
		LeftColorPresses = ColorCounts.ToArray();
		Presses = Enumerable.Range(0, LEDPositions.Length).Select(_ => -1).ToArray();
	}

	public void Press(int btnInd) {
		int ledInd = btnInd / 2;
		if (LitLEDs[ledInd]) return;
		LitLEDs[ledInd] = true;
		Presses[ledInd] = btnInd % 2;
		foreach (int colorIndex in ButtonColors[btnInd]) LeftColorPresses[colorIndex] -= 1;
	}

	private void SortLEDs() {
		for (int j = 0; j < LEDPositions.Length; j++) {
			for (int k = j + 1; k < LEDPositions.Length; k++) {
				int yDiff = LEDPositions[k].y - LEDPositions[j].y;
				if (yDiff > 0) continue;
				if (yDiff == 0 && LEDPositions[k].x > LEDPositions[j].x) continue;
				Swap(LEDPositions, j, k);
				Swap(ButtonPositions, 2 * j, 2 * k);
				Swap(ButtonPositions, 2 * j + 1, 2 * k + 1);
			}
		}
	}

	private int[] GenerateSolutionPath() {
		List<int> res = Enumerable.Range(0, COLORS_COUNT - 1).ToList();
		while (res.Count < Solution.Length) {
			res.Add(Random.Range(0, COLORS_COUNT - 1));
		}
		return res.Shuffle().ToArray();
	}

	private static void Swap<T>(T[] arr, int j, int k) {
		T temp = arr[j];
		arr[j] = arr[k];
		arr[k] = temp;
	}

	private static int Not(int num) {
		return num == 0 ? 1 : 0;
	}

	private static T PickFrom<T>(params T[] args) {
		return args.PickRandom();
	}
}
