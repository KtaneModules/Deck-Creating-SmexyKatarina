using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class DeckCreatingScript : MonoBehaviour {

	// External Vars
	public KMBombInfo _bomb;
	public KMAudio _audio;

	public SpriteRenderer[] _cardRenderers;
	public KMSelectable[] _cardSelectors;
	public TextMesh _cardCounter;
	public TextMesh _cardName;
	public Sprite[] _cardBackings;

	// Hearthstone
	public Sprite[] _avianaDruid;
	public Sprite[] _controlWarrior;
	public Sprite[] _exodiaMage;
	public Sprite[] _faceHunter;
	public Sprite[] _miracleRogue;

	// MTG
	public Sprite[] _blue;
	public Sprite[] _green;
	public Sprite[] _red;
	public Sprite[] _white;

	// Pokemon
	public Sprite[] _torchic;
	public Sprite[] _mudkip;
	public Sprite[] _flygon;
	public Sprite[] _treecko;

	// Specifically for Logging
	static int _modIDCount = 1;
	int _modID;
	private bool _modSolved;

	// Internal Vars
	Sprite[] _chosenDeckSprites;

	int[] _chosenDeckArray;
	int _chosenCardGame = 0;
	int _chosenDeckIndex = 0;
	int _cardSet = 0;
	int _correctCard = 0;

	float _chosenCardValue = 0;

	string _chosenCardName = "";
	string _chosenCardDeck = "";

	bool timer;
	bool _isAnimating;
	bool _starting;

	int[][] _deckCards = new int[][]
	{
		// Hearthstone
		new int[] { 1, 1, 2, 2, 1, 2, 1, 1, 2, 1, 2, 1, 2, 2, 2, 2, 2, 2, 1 }, // Aviana Druid 30
		new int[] { 1, 1, 2, 1, 1, 2, 2, 2, 1, 1, 2, 2, 2, 1, 1, 1, 2, 2, 2, 1 }, // Control Warrior 30
		new int[] { 2, 2, 2, 2, 1, 2, 2, 1, 2, 2, 1, 1, 2, 2, 2, 2, 2 }, // Face Hunter 30
		new int[] { 2, 2, 1, 2, 2, 1, 2, 2, 2, 1, 2, 2, 2, 2, 2, 1, 2 }, // Miracle Rogue 30
		// MTG
		new int[] { 1, 1, 1, 2, 2, 13, 1, 1, 1, 1, 1, 1, 2, 1, 1 }, // Blue 30
		new int[] { 1, 1, 2, 13, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 2 }, // Green 30
		new int[] { 2, 1, 1, 1, 2, 1, 1, 13, 1, 2, 1, 2, 2 }, // Red 30
		new int[] { 1, 1, 1, 2, 1, 1, 1, 2, 1, 13, 1, 2, 1, 1, 1 }, // White 30
		// Pokemon
		new int[] { 1, 2, 1, 2, 1, 5, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 2, 5 }, // Torchic missing fire energy
		new int[] { 2, 1, 1, 1, 1, 2, 1, 2, 1, 1, 1, 1, 1, 5, 2, 1, 1, 5 }, // Mudkip 30
		new int[] { 1, 1, 1, 5, 1, 5, 1, 2, 1, 2, 1, 1, 1, 1, 1, 2, 1, 2 }, // Flygon 30
		new int[] { 1, 1, 1, 5, 5, 2, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 2, 2 }, // Treecko
		// Extra
		new int[] { 2, 2, 2, 1, 2, 2, 2, 1, 2, 2, 1, 2, 1, 2, 2, 2, 2 } // Exodia Mage 26 2 missing pic research ice block
	};

	float[][] _valuesTable = new float[][]
	{
		new float[] { 13.5f, 14.2f, 14.2f, 2f },
		new float[] { 14.2f, 102f, 2f, 102f },
		new float[] { 2f, 13.5f, 102f, 2f },
		new float[] { 14.2f, 2f, 102f, 13.5f },
	};
	float[] _valueArray = new float[] { 13.5f, 14.2f, 2f, 102f };

	string[][] _deckTable = new string[][]
	{
		new string[] { "Aviana Druid", "Face Hunter", "Control Warrior", "Miracle Rogue" },
		new string[] { "Blue", "Red", "Green", "White" },
		new string[] { "Torchic Constructed", "Mudkip Constructed", "Flygon Constructed", "Treecko Constructed" }
	};
	string[] _games = new string[] { "Hearthstone", "Magic The Gathering", "Pokemon" };

	void Awake() {
		_modID = _modIDCount++;

		int count = 0;
		foreach (KMSelectable km in _cardSelectors) {
			km.OnInteract += delegate () { if (_isAnimating) { return false; } if (_modSolved) { return false; } CardCheck(km); return false; };
			if (count != 3) {
				km.OnHighlight += delegate () { if (_isAnimating) { return; } HighlightName(km); return; };
				km.OnHighlightEnded += delegate () { if (_isAnimating) { return; } _cardName.text = ""; return; };
			}
		}
	}

	void Start() {
		StartCoroutine(DisableHighlights());
		StartModule();
		GetSumOfSNum();
	}

	void CardCheck(KMSelectable button) {
		int index = Array.IndexOf(_cardSelectors, button);
		if (index == 3) {
			PrintDebug("Resetting module due to reset button being pressed{0}", new object[] { "." });
			StartModule();
			_cardSet = 0;
			_cardCounter.text = "";
			return;
		}
		if (index != _correctCard) {
			PrintDebug("Unfortunately, {0}{1} ({2}) card is the incorrect card. Resetting...", new object[] { index + 1, GetPositionOfNum(index), _cardRenderers[index].sprite.name });
			GetComponent<KMBombModule>().HandleStrike();
			StartModule();
			_cardSet = 0;
			return;
		}
		PrintDebug("Correct card selected, generating new set of cards{0}", new object[] { "." });
		StartCoroutine(GenerateNewCards());
	}

	void HighlightName(KMSelectable button) {
		int index = Array.IndexOf(_cardSelectors, button);
		switch (index) {
			case 0:
				_cardName.text = _cardRenderers[0].sprite.name;
				break;
			case 1:
				_cardName.text = _cardRenderers[1].sprite.name;
				break;
			case 2:
				_cardName.text = _cardRenderers[2].sprite.name;
				break;
			default:
				break;
		}
	}

	void StartModule() {
		int chosen = rnd.Range(0, 3);
		_chosenCardGame = chosen;
		_chosenCardName = _games[_chosenCardGame];
		PrintDebug("The chosen card game is: {0}.", new string[] { _games[_chosenCardGame] });
		// Show randomizing cards here for a little
		GenerateValue();
		StartCoroutine(RandomizeCards());
	}

	void GenerateValue() {
		if ((_bomb.GetBatteryCount() == _bomb.GetBatteryHolderCount()) && _bomb.GetIndicators().Any(x => x.Equals("BOB"))) {
			_chosenCardDeck = "Exodia Mage";
			_chosenCardName = "Hearthstone";
			_chosenCardGame = 0;
			_chosenDeckSprites = _exodiaMage;
			_chosenCardValue = 42069;
			_chosenDeckArray = _deckCards[12];
			_chosenDeckIndex = 12;
			PrintDebug("The chosen game has been changed to: {0}", new object[] { _chosenCardName });
			PrintDebug("The chosen value is: {0}.", new string[] { _chosenCardValue.ToString() });
			PrintDebug("The deck being created is: {0}", new string[] { "EXODIA!" });

			return;
		}
		int top = 0;
		int bot = 0;
		if (_bomb.GetOffIndicators().Count() > _bomb.GetOnIndicators().Count()) {
			if (_bomb.GetSerialNumberLetters().Count() == 0) {
				top = 1 * _bomb.GetOffIndicators().Count();
			} else {
				top = AlphaPosition(_bomb.GetSerialNumberLetters().Last()) * _bomb.GetOffIndicators().Count();
			}
			if (_bomb.GetPortCount(Port.StereoRCA) >= 1 && _bomb.GetPortCount(Port.Serial) == 0) {
				top *= 2;
			}
			top %= 4;
			bot = (GetSumOfSNum() % 4);
			_chosenCardValue = _valuesTable[top][bot];
		} else if (_bomb.GetOffIndicators().Count() < _bomb.GetOnIndicators().Count()) {
			top = ((_bomb.GetOnIndicators().Count() + _bomb.GetPortPlateCount() + 6) % 4);
			bot = ((IndiInString(_chosenCardName, true) + IndiInString(_chosenCardName, false) + _bomb.GetBatteryCount() + (ModuleOnBomb("monsplodeCards") || ModuleOnBomb("monsplodeFight") ? 13 : 0)) % 4);
			_chosenCardValue = _valuesTable[top][bot];
		} else if (_bomb.GetOffIndicators().Count() == _bomb.GetOnIndicators().Count()) {
			top = ((_bomb.GetBatteryCount() * (_bomb.GetPortCount() + _bomb.GetPortPlateCount())) % 4);
			bot = 0;
			_chosenCardValue = _valuesTable[top][bot];
		}
		PrintDebug("The chosen value (from table 1) is: {0} (Row = {1}, Column = {2}).", new object[] { _chosenCardValue.ToString(), top, bot });
		GenerateDeck();
	}

	void GenerateDeck() {
		_chosenCardDeck = _deckTable[Array.IndexOf(_games, _chosenCardName)][Array.IndexOf(_valueArray, _chosenCardValue)];
		GetSpriteSize();
		switch (_chosenCardDeck) {
			case "Aviana Druid":
				_chosenDeckSprites = _avianaDruid;
				_chosenDeckIndex = 0;
				break;
			case "Face Hunter":
				_chosenDeckSprites = _faceHunter;
				_chosenDeckIndex = 2;
				break;
			case "Control Warrior":
				_chosenDeckSprites = _controlWarrior;
				_chosenDeckIndex = 1;
				break;
			case "Miracle Rogue":
				_chosenDeckSprites = _miracleRogue;
				_chosenDeckIndex = 3;
				break;
			case "Blue":
				_chosenDeckSprites = _blue;
				_chosenDeckIndex = 4;
				break;
			case "Red":
				_chosenDeckSprites = _red;
				_chosenDeckIndex = 6;
				break;
			case "Green":
				_chosenDeckSprites = _green;
				_chosenDeckIndex = 5;
				break;
			case "White":
				_chosenDeckSprites = _white;
				_chosenDeckIndex = 7;
				break;
			case "Torchic Constructed":
				_chosenDeckSprites = _torchic;
				_chosenDeckIndex = 8;
				break;
			case "Mudkip Constructed":
				_chosenDeckSprites = _mudkip;
				_chosenDeckIndex = 9;
				break;
			case "Flygon Constructed":
				_chosenDeckSprites = _flygon;
				_chosenDeckIndex = 10;
				break;
			case "Treecko Constructed":
				_chosenDeckSprites = _treecko;
				_chosenDeckIndex = 11;
				break;
			default:
				break;
		}
		_chosenDeckArray = _deckCards[_chosenDeckIndex];
		PrintDebug("The deck being created (from table 2) is: {0}.", new string[] { _chosenCardDeck });
	}

	Sprite[] GetDeck(int id) {
		switch (id)
		{
			case 0:
				_cardRenderers[0].transform.localScale = GetSpriteSize(0);
				_cardRenderers[1].transform.localScale = GetSpriteSize(0);
				_cardRenderers[2].transform.localScale = GetSpriteSize(0);
				return _avianaDruid;
			case 1:
				_cardRenderers[0].transform.localScale = GetSpriteSize(0);
				_cardRenderers[1].transform.localScale = GetSpriteSize(0);
				_cardRenderers[2].transform.localScale = GetSpriteSize(0);
				return _controlWarrior;
			case 2:
				_cardRenderers[0].transform.localScale = GetSpriteSize(0);
				_cardRenderers[1].transform.localScale = GetSpriteSize(0);
				_cardRenderers[2].transform.localScale = GetSpriteSize(0);
				return _faceHunter;
			case 3:
				_cardRenderers[0].transform.localScale = GetSpriteSize(0);
				_cardRenderers[1].transform.localScale = GetSpriteSize(0);
				_cardRenderers[2].transform.localScale = GetSpriteSize(0);
				return _miracleRogue;
			case 4:
				_cardRenderers[0].transform.localScale = GetSpriteSize(1);
				_cardRenderers[1].transform.localScale = GetSpriteSize(1);
				_cardRenderers[2].transform.localScale = GetSpriteSize(1);
				return _blue;
			case 5:
				_cardRenderers[0].transform.localScale = GetSpriteSize(1);
				_cardRenderers[1].transform.localScale = GetSpriteSize(1);
				_cardRenderers[2].transform.localScale = GetSpriteSize(1);
				return _green;
			case 6:
				_cardRenderers[0].transform.localScale = GetSpriteSize(1);
				_cardRenderers[1].transform.localScale = GetSpriteSize(1);
				_cardRenderers[2].transform.localScale = GetSpriteSize(1);
				return _red;
			case 7:
				_cardRenderers[0].transform.localScale = GetSpriteSize(1);
				_cardRenderers[1].transform.localScale = GetSpriteSize(1);
				_cardRenderers[2].transform.localScale = GetSpriteSize(1);
				return _white;
			case 8:
				_cardRenderers[0].transform.localScale = GetSpriteSize(2);
				_cardRenderers[1].transform.localScale = GetSpriteSize(2);
				_cardRenderers[2].transform.localScale = GetSpriteSize(2);
				return _torchic;
			case 9:
				_cardRenderers[0].transform.localScale = GetSpriteSize(2);
				_cardRenderers[1].transform.localScale = GetSpriteSize(2);
				_cardRenderers[2].transform.localScale = GetSpriteSize(2);
				return _mudkip;
			case 10:
				_cardRenderers[0].transform.localScale = GetSpriteSize(2);
				_cardRenderers[1].transform.localScale = GetSpriteSize(2);
				_cardRenderers[2].transform.localScale = GetSpriteSize(2);
				return _flygon;
			case 11:
				_cardRenderers[0].transform.localScale = GetSpriteSize(2);
				_cardRenderers[1].transform.localScale = GetSpriteSize(2);
				_cardRenderers[2].transform.localScale = GetSpriteSize(2);
				return _treecko;
			default:
				return null;
		}
	}

	Sprite[] GetDeckFromGame(int id) {
		switch (_chosenCardName) {
			case "Hearthstone":
				if (id == _chosenDeckIndex) {
					if (id + 1 >= 4) {
						id--;
					} else {
						id++;
					}
				}
				return GetDeck(id);
			case "Magic The Gathering":
				if (id + 4 == _chosenDeckIndex) {
					if (id + 1 >= 4) {
						id--;
					} else {
						id++;
					}
				}
				return GetDeck(id + 4);
			case "Pokemon":
				if (id + 8 == _chosenDeckIndex) {
					if (id + 1 >= 4) {
						id--;
					} else {
						id++;
					}
				}
				return GetDeck(id + 8);
			default:
				return null;
		}
	}

	Vector3 GetSpriteSize() {
		switch (_chosenCardGame) {
			case 0:
				return new Vector3(0.032f, 0.032f, 1f);
			case 1:
				return new Vector3(0.025f, 0.025f, 1f);
			case 2:
				return new Vector3(0.023f, 0.023f, 1f);
			default:
				return new Vector3(-1f, -1f, -1f);
		}
	}

	Vector3 GetSpriteSize(int id) {
		switch (id) {
			case 0:
				return new Vector3(0.032f, 0.032f, 1f);
			case 1:
				return new Vector3(0.025f, 0.025f, 1f);
			case 2:
				return new Vector3(0.023f, 0.023f, 1f);
			default:
				return new Vector3(-1f, -1f, -1f);
		}
	}

	Vector3 GetButtonSize() {
		switch (_chosenCardGame) {
			case 0:
				return new Vector3(0.035f, 0.001f, 0.05f);
			case 1:
				return new Vector3(0.035f, 0.001f, 0.05f);
			case 2:
				return new Vector3(0.035f, 0.001f, 0.05f);
			default:
				return new Vector3(-1f, -1f, -1f);
		}
	}

	int IndiInString(string s, bool on) {
		int count = 0;
		if (on) {
			foreach (string indi in _bomb.GetOnIndicators()) {
				List<char> chars = s.ToCharArray().ToList();
				foreach (char c in indi) {
					if (chars.Contains(c)) {
						count++;
						break;
					}
				}
			}
			return count;
		}
		foreach (string indi in _bomb.GetOffIndicators()) {
			List<char> chars = s.ToCharArray().ToList();
			foreach (char c in indi) {
				if (chars.Contains(c)) {
					count++;
					break;
				}
			}
		}
		return count;
	}

	bool ModuleOnBomb(string id) {
		if (_bomb.GetModuleIDs().Contains(id)) {
			return true;
		}
		return false;
	}

	void PrintDebug(string s, object[] args) {
		Debug.LogFormat("[Deck Creating #{0}]: {1}", _modID, String.Format(s, args));
	}

	int AlphaPosition(char c) {
		return "abcdefghijklmnopqrstuvwxyz".IndexOf(c.ToString().ToLowerInvariant()) + 1;
	}

	int GetSumOfSNum() {
		int total = 0;
		foreach (char c in _bomb.GetSerialNumberLetters()) {
			total += AlphaPosition(c.ToString().ToLower().ToCharArray()[0]);
		}
		foreach (int i in _bomb.GetSerialNumberNumbers()) {
			total += i;
		}
		return total;
	}

	string GetPositionOfNum(int index) {
		switch (index) {
			case 0:
				return "st";
			case 1:
				return "nd";
			case 2:
				return "rd";
			default:
				return null;
		}
	}

	IEnumerator RandomizeCards() {
		_isAnimating = true;
		if (!_starting) {
			StartCoroutine(Timer(5.0f));
			_starting = true;
		} else {
			StartCoroutine(Timer(1.5f));
		}
		while (!timer) {
			int deck = rnd.Range(0, 12);
			Sprite[] sprites = GetDeck(deck);
			foreach (SpriteRenderer sr in _cardRenderers) {
				sr.sprite = null;
			}
			for (int i = 0; i <= 2; i++) {
				SpriteRenderer cardHolder = _cardRenderers[rnd.Range(0, 3)];
				cardHolder.sprite = sprites[rnd.Range(0, sprites.Length)];
			}
			foreach (SpriteRenderer sr in _cardRenderers) {
				if (sr.sprite == null) {
					sr.sprite = sprites[rnd.Range(0, sprites.Length)];
				}
			}
			yield return new WaitForSeconds(0.1f);
		}
		StartCoroutine(GenerateNewCards());
		int count = 0;
		foreach (KMSelectable km in _cardSelectors) {
			if (count == 3) { km.Highlight.gameObject.SetActive(true); continue; }
			km.GetComponent<Renderer>().transform.localScale = GetButtonSize();
			km.Highlight.gameObject.SetActive(true);
			count++;
		}
		_isAnimating = false;
		yield break;
	}

	IEnumerator Timer(float time) {
		timer = false;
		yield return new WaitForSeconds(time);
		timer = true;
		yield break;
	}

	IEnumerator DisableHighlights() {
		yield return new WaitForSeconds(0.7f);
		foreach (KMSelectable km in _cardSelectors) {
			km.Highlight.gameObject.SetActive(false);
		}
		yield break;
	}

	IEnumerator GenerateNewCards() {
		int rand = rnd.Range(0, _chosenDeckArray.Length);
		if (_chosenDeckArray.All(x => x == 0)) {
			GetComponent<KMBombModule>().HandlePass();
			_modSolved = true;
			PrintDebug("All correct cards have been selected. Module Solved{0}", new object[] { "." });
			StartCoroutine(FlipCards());
			yield break;
		}
		while (_chosenDeckArray[rand] == 0) {
			rand = rnd.Range(0, _chosenDeckArray.Length);
		}
		_chosenDeckArray[rand]--;
		int rPos = rnd.Range(0, 3);
		bool[] cPos = new bool[3];
		cPos[rPos] = true;
		_cardRenderers[rPos].sprite = _chosenDeckSprites[rand];
		_correctCard = rPos;
		List<Sprite> sprites = new List<Sprite>();
		List<int> indices = new List<int>();
		for (int i = 0; i <= 2; i++) {
			if (cPos[i]) { continue; }
			sprites.Clear();
			indices.Clear();
			indices.Add(rand);
			cPos[i] = true;
			Sprite[] randomSpr = GetDeckFromGame(rnd.Range(0, 4));
			int counter = 0;
			foreach (int x in _chosenDeckArray) {
				if (x == 0) {
					if (indices.Contains(counter)) {
						continue;
					}
					sprites.Add(_chosenDeckSprites[counter]);
					indices.Add(counter);
				}
				counter++;
			}
			foreach (Sprite s in randomSpr) {
				if (_chosenDeckSprites.Any(x => x.name.Equals(s.name))) { continue; }
				if (sprites.Any(x => x.name.Equals(s.name))) { continue; }
				sprites.Add(s);
			}
			_cardRenderers[i].sprite = sprites.ElementAt(rnd.Range(0, sprites.Count));
		}
		PrintDebug("Out of cards on set {0}, the correct card is {1} ({2}).", new object[] { _cardSet + 1, rPos + 1, _chosenDeckSprites[rand].name });
		_cardCounter.text = (_cardSet + 1).ToString();
		_cardSet++;
		yield break;
	}

	IEnumerator FlipCards() {
		foreach (KMSelectable km in _cardSelectors) {
			km.Highlight.gameObject.SetActive(false);
		}
		foreach (SpriteRenderer sr in _cardRenderers) {
			sr.sprite = _cardBackings[_chosenCardGame];
			yield return new WaitForSeconds(1.0f);
		}
		yield break;
	}

	#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} card 1/2/3 [Presses the card at the specfied position, chain by spaces] | !{0} reset [Presses the reset button] | !{0} names [Gives you the names of the cards] ";
	#pragma warning restore 414

	IEnumerator ProcessTwitchCommand(string command) {
		string[] split = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (!split[0].ToLower().EqualsAny("card", "reset", "names")) {
			yield return "sendtochaterror Invalid sub command.";
		}
		if (split[0].ToLower().Equals("card") && split.Length != 2) {
			yield return "sendtochaterror Invalid argument size.";
		}
		switch (split[0].ToLower()) {
			case "card":
				int result;
				if (!int.TryParse(split[1], out result)) {
					yield return "sendtochaterror Invalid number format.";
					break;
				}
				result = int.Parse(split[1]);
				if (!result.EqualsAny(1, 2, 3)) {
					yield return "sendtochaterror Invalid position";
					break;
				}
				yield return null;
				_cardSelectors[result - 1].OnInteract();
				break;
			case "reset":
				yield return null;
				_cardSelectors[3].OnInteract();
				break;
			case "names":
				yield return "sendtochat The names from left to right are: " + _cardRenderers[0].name + ", " + _cardRenderers[1].name + " and " + _cardRenderers[2].name;
				break;
			default:
				yield return "sendtochaterror Invalid command.";
				break;
		}
		yield break;
	}

	IEnumerator TwitchHandleForcedSolve() {
		yield return null;
		while (_cardSet != 29) {
			if (_isAnimating) { continue; }
			_cardSelectors[_correctCard].OnInteract();
			yield return new WaitForSeconds(0.5f);
		}
		yield break;
	}
}
