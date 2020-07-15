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

	string _chosenCardGameName = "";
	string _chosenCardDeck = "";

	bool timer;
	bool _isAnimating;
	bool _starting;
	bool _notFirst;
	bool _chosenGame = false;

	int[][] _deckCards = new int[][]
	{
		// Hearthstone 0-3
		new int[] { 1, 1, 2, 2, 1, 2, 1, 1, 2, 1, 2, 1, 2, 2, 2, 2, 2, 2, 1 }, // Aviana Druid
		new int[] { 1, 1, 2, 1, 1, 2, 2, 2, 1, 1, 2, 2, 2, 1, 1, 1, 2, 2, 2, 1 }, // Control Warrior
		new int[] { 2, 2, 2, 2, 1, 2, 2, 1, 2, 2, 1, 1, 2, 2, 2, 2, 2 }, // Face Hunter
		new int[] { 2, 2, 1, 2, 2, 1, 2, 2, 2, 1, 2, 2, 2, 2, 2, 1, 2 }, // Miracle Rogue
		// MTG 4-7
		new int[] { 1, 1, 1, 2, 2, 13, 1, 1, 1, 1, 1, 1, 2, 1, 1 }, // Blue
		new int[] { 1, 1, 2, 13, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 2 }, // Green
		new int[] { 2, 1, 1, 1, 2, 1, 1, 13, 1, 2, 1, 2, 2 }, // Red
		new int[] { 1, 1, 1, 2, 1, 1, 1, 2, 1, 13, 1, 2, 1, 1, 1 }, // White
		// Pokemon 8-11
		new int[] { 1, 2, 1, 2, 1, 5, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 2, 5 }, // Torchic
		new int[] { 2, 1, 1, 1, 1, 2, 1, 2, 1, 1, 1, 1, 1, 5, 2, 1, 1, 5 }, // Mudkip
		new int[] { 1, 1, 1, 5, 1, 5, 1, 2, 1, 2, 1, 1, 1, 1, 1, 2, 1, 2 }, // Flygon
		new int[] { 1, 1, 1, 5, 5, 2, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 2, 2 }, // Treecko
		// Extra 12
		new int[] { 2, 2, 2, 1, 2, 2, 2, 1, 2, 2, 1, 2, 1, 2, 2, 2, 2 } // Exodia Mage
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
			km.OnInteract += delegate () { if (_isAnimating || _modSolved) { return false; } CardCheck(km); return false; };
			if (count != 3) {
				km.OnHighlight += delegate () { if (_isAnimating || _modSolved) { return; } HighlightName(km); return; };
				km.OnHighlightEnded += delegate () { if (_isAnimating || _modSolved) { return; } _cardName.text = ""; return; };
			}
		}
	}

	void Start() {
		StartCoroutine(DisableHighlights());
		StartModule();
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
		foreach (SpriteRenderer sr in _cardRenderers) {
			sr.sprite = null;
			sr.transform.localScale = GetSpriteSize();
		}
		_cardName.text = "";
		_notFirst = true;
		StartCoroutine(GenerateNewSet());
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
		if (!_chosenGame) 
		{
			
			

			
			if (_bomb.GetBatteryCount() == _bomb.GetBatteryHolderCount() && _bomb.GetOnIndicators().Any(x => x == "BOB"))
			{
				_chosenCardGame = 0;
				_chosenCardGameName = _games[_chosenCardGame];
				_chosenCardDeck = "Exodia Mage";
				_chosenDeckSprites = _exodiaMage;
				_chosenDeckIndex = 12;
				_chosenDeckArray = _deckCards[_chosenDeckIndex];
				PrintDebug("The selected card game is {0}.", new string[] { _games[_chosenCardGame] });
				PrintDebug("Bob is here to show you how to build the Exodia Mage deck. But, careful he has limited batteries{0}", new object[] { "." });
				StartCoroutine(RandomizeCards());
				return;
			}
			else 
			{
				_chosenCardGame = chosen;
				_chosenCardGameName = _games[_chosenCardGame];
				GenerateValue();
				PrintDebug("The selected card game is {0}.", new string[] { _games[_chosenCardGame] });
			}
			
			_chosenGame = true;
		}
		StartCoroutine(RandomizeCards());
	}

	void GenerateValue() {
		int rule = GetIndicatorRule();

		string[][] logs = new string[][]
		{
			new string[] { "more unlit indicators than lit indicators." },
			new string[] { "more lit indicators than unlit indicators." },
			new string[] { "an equal amount of indicators." }
		};

		PrintDebug("There is {0}", logs[rule]);

		int row = 0;
		int col = 0;

		char[] alphabet = Enumerable.Range(0, 26).Select(x => (char)(x + 'A')).ToArray();

		switch (rule) 
		{
			case 0: // unlit > lit
				switch (_chosenCardGameName) 
				{
					case "Hearthstone":
						row = _bomb.GetBatteryCount() + _bomb.GetSerialNumberNumbers().Last();
						col = (_bomb.GetPortPlateCount() + _bomb.GetPortCount()) * _bomb.GetSerialNumberNumbers().First();
						break;
					case "Magic The Gathering":
						row = Array.IndexOf(alphabet, _bomb.GetSerialNumberLetters().First()) + 1;
						col = Array.IndexOf(alphabet, _bomb.GetSerialNumberLetters().Last()) + 1;
						break;
					case "Pokemon":
						row = _bomb.GetSerialNumberNumbers().Sum();
						col = _bomb.GetSolvableModuleIDs().Count();
						break;
					default:
						break;
				}
				break;
			case 1: // unlit < lit
				switch (_chosenCardGameName)
				{
					case "Hearthstone":
						row = _bomb.GetPortCount() + _bomb.GetSerialNumberNumbers().Last();
						col = (_bomb.GetBatteryHolderCount() + _bomb.GetBatteryCount()) * _bomb.GetSerialNumberNumbers().First();
						break;
					case "Magic The Gathering":
						row = _bomb.GetOnIndicators().Count();
						col = _bomb.GetOffIndicators().Count();
						break;
					case "Pokemon":
						row = _bomb.GetSerialNumberLetters().Select(x => Array.IndexOf(alphabet, x)+1).Sum();
						col = _bomb.GetSolvedModuleIDs().Count();
						break;
					default:
						break;
				}
				break;
			default: // lit == unlit
				switch (_chosenCardGameName)
				{
					case "Hearthstone":
						row = _bomb.GetBatteryHolderCount() + _bomb.GetSerialNumberNumbers().Last();
						col = (_bomb.GetIndicators().Count()) * _bomb.GetSerialNumberNumbers().First();
						break;
					case "Magic The Gathering":
						row = (_bomb.GetPortCount(Port.Serial)+_bomb.GetPortCount(Port.Parallel)+_bomb.GetPortCount(Port.StereoRCA)) * (_bomb.GetBatteryCount() + _bomb.GetBatteryHolderCount());
						col = (_bomb.GetPortCount(Port.RJ45) + _bomb.GetPortCount(Port.DVI) + _bomb.GetPortCount(Port.PS2)) * _bomb.GetIndicators().Count();
						break;
					case "Pokemon":
						row = _bomb.GetSerialNumberNumbers().Sum() + _bomb.GetSerialNumberLetters().Select(x => Array.IndexOf(alphabet, x) + 1).Sum();
						col = _bomb.GetSolvableModuleIDs().Count() - _bomb.GetSolvedModuleIDs().Count();
						break;
					default:
						break;
				}
				break;
		}

		_chosenCardValue = _valuesTable[row%4][col%4];

		PrintDebug("The chosen value is {0}. The row is {1} ({2} after modulo + 1). The column is {3} ({4} after modulo + 1).", new object[] { _chosenCardValue, row, (row%4)+1, col, (col%4)+1, });

		GenerateDeck();
	}

	void GenerateDeck() {
		_chosenCardDeck = _deckTable[Array.IndexOf(_games, _chosenCardGameName)][Array.IndexOf(_valueArray, _chosenCardValue)];
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

	int GetIndicatorRule() 
	{
		return _bomb.GetOffIndicators().Count() > _bomb.GetOnIndicators().Count() ? 0 : _bomb.GetOffIndicators().Count() < _bomb.GetOnIndicators().Count() ? 1 : 2;
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
		switch (_chosenCardGameName) {
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

	void PrintDebug(string s, object[] args) {
		Debug.LogFormat("[Deck Creating #{0}]: {1}", _modID, String.Format(s, args));
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
		StartCoroutine(GenerateNewSet());
		int count = 0;
		foreach (KMSelectable km in _cardSelectors) {
			if (count == 3) { km.Highlight.gameObject.SetActive(true); continue; }
			km.GetComponent<Renderer>().transform.localScale = GetButtonSize();
			km.GetComponent<Renderer>().enabled = true;
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

	IEnumerator GenerateNewSet()
	{
		_isAnimating = true;
		if (_notFirst)
		{
			yield return new WaitForSeconds(0.3f);
		}
		if (_chosenDeckArray.All(x => x == 0))
		{
			GetComponent<KMBombModule>().HandlePass();
			_cardName.text = "";
			_modSolved = true;
			PrintDebug("All correct cards have been selected. Module Solved{0}", new object[] { "." });
			StartCoroutine(FlipCards());
			_isAnimating = false;
			yield break;
		}
		Sprite[] selectedCards = new Sprite[3];
		Sprite[] rendererCards = new Sprite[3];
		int generateCard = rnd.Range(0, _chosenDeckArray.Length);
		while (_chosenDeckArray[generateCard] == 0)
		{
			generateCard = rnd.Range(0, _chosenDeckArray.Length);
		}
		selectedCards[0] = _chosenDeckSprites[generateCard];
		_chosenDeckArray[generateCard]--;
		for (int i = 1; i <= 2; i++)
		{
			List<Sprite> randomSprites = GetDeckFromGame(rnd.Range(0, 4)).ToList();
			for (int z = 0; z <= _chosenDeckSprites.Length - 1; z++)
			{
				if (_chosenDeckArray[z] != 0) continue;
				randomSprites.Add(_chosenDeckSprites[z]);
			}
			Sprite chosen = randomSprites[rnd.Range(0, randomSprites.Count())];
			while (selectedCards[0].name == chosen.name || _chosenDeckSprites.Any(x => x.name == chosen.name))
			{
				chosen = randomSprites[rnd.Range(0, randomSprites.Count())];
			}
			selectedCards[i] = chosen;
		}
		bool[] usedPos = new bool[3];
		bool[] usedCard = new bool[3];
		int pos = rnd.Range(0, 3);
		int card = rnd.Range(0, 3);
		foreach (SpriteRenderer sr in _cardRenderers)
		{
			sr.sprite = null;
			sr.transform.localScale = GetSpriteSize();
		}
		for (int i = 0; i <= 2; i++)
		{
			while (usedPos[pos])
			{
				pos = rnd.Range(0, 3);
			}
			while (usedCard[card])
			{
				card = rnd.Range(0, 3);
			}
			_cardRenderers[pos].sprite = selectedCards[card];
			usedPos[pos] = true;
			usedCard[card] = true;
			rendererCards[pos] = selectedCards[card];
			yield return new WaitForSeconds(0.3f);
		}
		_correctCard = Array.IndexOf(rendererCards, selectedCards[0]);
		PrintDebug("Out of cards on set {0}, the correct card is {1} ({2}).", new object[] { _cardSet + 1, _correctCard + 1, rendererCards[_correctCard].name });
		_cardSet++;
		_cardCounter.text = _cardSet.ToString();
		_isAnimating = false;
		yield break;
	}

	IEnumerator FlipCards() {
		foreach (KMSelectable km in _cardSelectors) {
			km.Highlight.gameObject.SetActive(false);
			if (Array.IndexOf(_cardSelectors, km) == 3) continue;
			km.GetComponent<Renderer>().enabled = false;
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
			yield break;
		}
		if (split[0].ToLower().Equals("card") && split.Length != 2) {
			yield return "sendtochaterror Invalid argument size.";
			yield break;
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
					yield return "sendtochaterror Invalid position.";
					break;
				}
				yield return null;
				_cardSelectors[result - 1].OnInteract();
				yield return "solve";
				break;
			case "reset":
				yield return null;
				_cardSelectors[3].OnInteract();
				break;
			case "names":
				yield return "sendtochat The names from left to right are: " + _cardRenderers[0].sprite.name + ", " + _cardRenderers[1].sprite.name + " and " + _cardRenderers[2].sprite.name;
				break;
			default:
				yield return "sendtochaterror Invalid command.";
				break;
		}
		yield break;
	}

	IEnumerator TwitchHandleForcedSolve() {
		yield return null;
		while (_cardSet != 31) {
			if (_isAnimating) { yield return true; continue; }
			_cardSelectors[_correctCard].OnInteract();
			yield return new WaitForSeconds(0.5f);
		}
		yield break;
	}

}
