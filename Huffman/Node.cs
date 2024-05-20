namespace Huffman;

public class Node {
	public int Frequency { get; set; }
	
	public char? Character { get; set; }
	
	public Node? Left { get; set; }
	public Node? Right { get; set; }
}