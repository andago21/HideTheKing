using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public Transform[] squares;
    
    public GameObject whitePawn;
    public GameObject whiteRook;
    public GameObject whiteKnight;
    public GameObject whiteBishop;
    public GameObject whiteQueen;
    public GameObject whiteKing;
    
    public GameObject blackPawn;
    public GameObject blackRook;
    public GameObject blackKnight;
    public GameObject blackBishop;
    public GameObject blackQueen;
    public GameObject blackKing;

    void Start()
    {
        SetupBoard();
    }

    void SetupBoard()
    {
        // Bauern
        for (int i = 0; i < 8; i++)
        {
            Instantiate(whitePawn, squares[8 + i].position, Quaternion.identity, squares[8 + i]);
            
            Instantiate(blackPawn, squares[48 + i].position, Quaternion.identity, squares[48 + i]);
        }

        // Türme
        Instantiate(whiteRook, squares[0].position, Quaternion.identity, squares[0]);
        Instantiate(whiteRook, squares[7].position, Quaternion.identity, squares[7]);
        
        Instantiate(blackRook, squares[56].position, Quaternion.identity, squares[56]);
        Instantiate(blackRook, squares[63].position, Quaternion.identity, squares[63]);

        // Springer
        Instantiate(whiteKnight, squares[1].position, Quaternion.identity, squares[1]);
        Instantiate(whiteKnight, squares[6].position, Quaternion.identity, squares[6]);
        
        Instantiate(blackKnight, squares[57].position, Quaternion.identity, squares[57]);
        Instantiate(blackKnight, squares[62].position, Quaternion.identity, squares[62]);

        // Läufer
        Instantiate(whiteBishop, squares[2].position, Quaternion.identity, squares[2]);
        Instantiate(whiteBishop, squares[5].position, Quaternion.identity, squares[5]);
        
        Instantiate(blackBishop, squares[58].position, Quaternion.identity, squares[58]);
        Instantiate(blackBishop, squares[61].position, Quaternion.identity, squares[61]);

        // Damen
        Instantiate(whiteQueen, squares[3].position, Quaternion.identity, squares[3]);
        
        Instantiate(blackQueen, squares[59].position, Quaternion.identity, squares[59]);

        // Könige
        Instantiate(whiteKing, squares[4].position, Quaternion.identity, squares[4]);
        
        Instantiate(blackKing, squares[60].position, Quaternion.identity, squares[60]);
    }
}

