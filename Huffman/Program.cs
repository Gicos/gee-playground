using Huffman;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;

class Program {
	static void Main(string[] args) {
		FileStream inputPlaintext = new("input.txt", FileMode.Open);
		using StreamReader streamReader = new(inputPlaintext);

		IDictionary<char, Node> frequencyTable = BuildFrequencyTable(streamReader);
		Node huffmanTree = BuildHuffmanTree(frequencyTable);

		if (inputPlaintext.CanSeek) {
			inputPlaintext.Seek(0, SeekOrigin.Begin);
		} else {
			inputPlaintext.Dispose();
			inputPlaintext = new FileStream("input.txt", FileMode.Open);
		}

		using StreamReader streamReaderForCompression = new(inputPlaintext);
		(byte[] compressedBytes, int totalBits) = Compress(streamReaderForCompression, huffmanTree);
		inputPlaintext.Dispose();

		FileStream outputCompressed = new("output.bin", FileMode.Create);
		outputCompressed.Write(compressedBytes, 0, compressedBytes.Length);
		outputCompressed.Flush();
		outputCompressed.Dispose();

		byte[] inputCompressed = File.ReadAllBytes("output.bin");
		StringBuilder decompressedText = Decompress(inputCompressed, totalBits, huffmanTree);

		using FileStream outputDecompressed = new("output.txt", FileMode.Create);
		using StreamWriter streamWriter = new(outputDecompressed);
		streamWriter.Write(decompressedText.ToString());
	}

	private static IDictionary<char, Node> BuildFrequencyTable(StreamReader streamReader) {
		Dictionary<char, Node> frequencyTable = new();
		while (!streamReader.EndOfStream) {
			char c = (char) streamReader.Read();
			if (frequencyTable.TryGetValue(c, out Node? value)) {
				value.Frequency++;
			} else {
				frequencyTable[c] = new Node { Frequency = 1, Character = c };
			}
		}

		return frequencyTable;
	}

	private static Node BuildHuffmanTree(IDictionary<char, Node> frequencyTable) {
		PriorityQueue<Node, int> priorityQueue = new(frequencyTable.Count);
		if (frequencyTable.Count == 0) {
			return new Node();
		}

		foreach (KeyValuePair<char, Node> entry in frequencyTable) {
			priorityQueue.Enqueue(entry.Value, entry.Value.Frequency);
		}

		if (priorityQueue.Count == 1) {
			Node element = priorityQueue.Dequeue();
			Node parent = new() { Frequency = element.Frequency, Left = element, Right = null };
			priorityQueue.Enqueue(parent, parent.Frequency);
		} else {
			while (priorityQueue.Count > 1) {
				Node element1 = priorityQueue.Dequeue();
				Node element2 = priorityQueue.Dequeue();
				Node parent = new() { Frequency = element1.Frequency + element2.Frequency, Left = element1, Right = element2 };
				priorityQueue.Enqueue(parent, parent.Frequency);
			}
		}

		return priorityQueue.Dequeue();
	}

	private static void BuildHuffmanTable(Node node, BitArray prefix, Dictionary<char, BitArray> table) {
		if (node.Character is not null) {
			table.Add(node.Character.Value, new BitArray(prefix));
			return;
		}

		if (node.Left is not null) {
			BitArray la = new(prefix);
			la.Length += 1;
			la[^1] = false;
			BuildHuffmanTable(node.Left, la, table);
		}

		if (node.Right is not null) {
			BitArray ra = new(prefix);
			ra.Length += 1;
			ra[^1] = true;
			BuildHuffmanTable(node.Right, ra, table);
		}
	}

	private static (byte[] compressedBytes, int totalBits) Compress(StreamReader streamReader, Node root) {
		Dictionary<char, BitArray> huffmanTable = new();
		BuildHuffmanTable(root, new BitArray(0), huffmanTable);

		List<bool> compressedText = [];

		while (!streamReader.EndOfStream) {
			char c = (char) streamReader.Read();

			BitArray compressedChar = huffmanTable[c];

			for (int i = 0; i < compressedChar.Length; i++) {
				compressedText.Add(compressedChar[i]);
			}
		}

		byte[] compressedBytes = new byte[(compressedText.Count + 7) / 8];
		BitArray compressedBits = new(compressedText.ToArray());
		compressedBits.CopyTo(compressedBytes, 0);

		return (compressedBytes, compressedText.Count);
	}

	private static StringBuilder Decompress(byte[] bytes, int bits, Node root) {
		BitArray compressedBits = new(bytes);

		StringBuilder decompressedText = new();

		Node? currentNode = root;
		for (int i = 0; i < bits; i++) {
			if (currentNode is null) {
				continue;
			}

			currentNode = compressedBits[i] ? currentNode.Right : currentNode.Left;

			if (currentNode?.Character is null) {
				continue;
			}

			decompressedText.Append(currentNode.Character);
			currentNode = root;
		}

		return decompressedText;
	}
}