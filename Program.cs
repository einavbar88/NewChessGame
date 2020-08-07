using System;
using System.Collections;

namespace NewChessGame
{
    class ChessGameLauncher
    {
        static void Main(string[] args)
        {
            new Game().Play();
        }
    }

    class Game
    {
        public ChessPiece[,] board;
        int fiftyMovesTie = 0;
        bool isOver = false, isActiveCheck = false;
        bool isOverNotEnoughPieces = false, isOverThreeRepeatitiveBoards = false;
        bool hasRookOnA1Moved = false, hasRookOnA8Moved = false, hasRookOnH1Moved = false, hasRookOnH8Moved = false;
        bool hasWhiteKingMoved = false, hasBlackKingMoved = false;
        ArrayList savedBoards = new ArrayList();
        ArrayList isWhiteTurnOnSavedBoards = new ArrayList();
        ///////////////initialization///////////////
        public Game()
        {
            board = InitializeBoard();
            threeRepeatitiveBoardsSave(true);
        }
        ChessPiece[,] InitializeBoard()
        {
            ChessPiece[,] ret = new ChessPiece[,]
            {
                { new Rook(true), new Knight(true), new Bishop(true), new Queen(true), new King(true), new Bishop(true), new Knight(true), new Rook(true)},
                { new WhitePawn(), new WhitePawn(), new WhitePawn(), new WhitePawn(), new WhitePawn(), new WhitePawn(), new WhitePawn(), new WhitePawn(), },
                { new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece() },
                { new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece() },
                { new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece() },
                { new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece(), new ChessPiece() },
                { new BlackPawn(), new BlackPawn(), new BlackPawn(), new BlackPawn(), new BlackPawn(), new BlackPawn(), new BlackPawn(), new BlackPawn()},
                { new Rook(false), new Knight(false), new Bishop(false), new Queen(false), new King(false), new Bishop(false), new Knight(false), new Rook(false)}
            };
            return ret;
        }
        ////////////////////turn////////////////////
        public void Play()
        {
            bool isWhiteTurn = true;
            while (!isOver)
            {
                Move turn;
                PrintBoard();
                if (isActiveCheck) { Console.WriteLine("Check!"); }
                try
                {
                    turn = Turn(isWhiteTurn);
                    moveExecution(turn);
                }
                catch (TieRequestException) { isOver = isMutualAgreedTie(); break; }
                catch (CastleException)
                {
                    if (!hasCastlingSucceded(isWhiteTurn))
                    {
                        Console.WriteLine("Castling have failed! Make sure you are folowing the game rules");
                        continue;
                    }
                }
                catch (IllegalMoveException) { Console.WriteLine("Enter a valid move"); continue; }
                catch (CheckException) { Console.WriteLine("Your king is threatend! try a different move"); continue; }
                enPassantReset(isWhiteTurn);
                isWhiteTurn = !isWhiteTurn;
                isOver = isGameOver(isWhiteTurn);
            }
        }
        void PrintBoard()
        {
            char[] num = { ' ', ' ', '8', '7', '6', '5', '4', '3', '2', '1' };
            char[] letters = { ' ', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H' };
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (i == 0 && j == 0 || i == 1)
                        Console.Write("   ");
                    else if (i == 0)
                        Console.Write(letters[j] + "   ");
                    else if (j == 0)
                        Console.Write(num[i] + "  ");
                    else
                        Console.Write(board[(11 - i) - 2, j - 1] + "  ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
        Move Turn(bool isWhiteTurn)
        {
            Location from = null, target = null;
            Move ret = new Move();
            try
            {
                from = inputRecieve(isWhiteTurn, true);
                if (from != null)
                {
                    target = inputRecieve(isWhiteTurn, false);
                    ret = new Move(from, target);
                }
                ChessPiece[,] hypotheticBoard = applyingMoveOnHypotheticBoardIfLegal(board, ret, !isWhiteTurn);
                if (hypotheticBoard != null && isValidMoveNoCheck(hypotheticBoard, !isWhiteTurn))
                    return ret;
                throw new IllegalMoveException();
            }
            catch (TieRequestException) { throw; }
            catch (CastleException) { throw; }
            catch (IllegalMoveException) { throw; }
            catch (CheckException) { throw; }
        }
        Location inputRecieve(bool isWhiteTurn, bool isFrom)
        {
            Location ret = null;
            do
            {
                Console.WriteLine(isFrom ? string.Format("{0} Turn:", isWhiteTurn ? "Player 1[W]" : "Player 2[B]") : "Enter target");
                string input = Console.ReadLine();

                try { ret = inputTranslation(input, isWhiteTurn, isFrom); }

                catch (TieRequestException) { throw; }
                catch (CastleException) { throw; }
                catch (IllegalMoveException) { throw; }
                catch (ArgumentException) { Console.WriteLine("Enter a valid location"); }

            } while (ret == null);
            return ret;
        }
        Location inputTranslation(string userInput, bool isWhiteTurn, bool isFrom)
        {
            string input = userInput.Trim();
            input = input.ToUpper();
            if (input == "TIE")
                throw new TieRequestException();

            else if (input == "CASTLE")
                throw new CastleException();

            else if (input.Length != 2 || input[1] < 49 || input[1] > 56 || input[0] < 64 || input[0] > 72)
                throw new ArgumentException();

            int x = input[1] - 49;
            int y = input[0] - 65;

            if ((board[x, y].IsWhite == isWhiteTurn && isFrom && !board[x, y].IsEmpty) || (!isFrom && (board[x, y].IsEmpty || (board[x, y].IsWhite == !isWhiteTurn))))
                return new Location(x, y);

            throw new IllegalMoveException();
        }
        bool isLegalMove(ChessPiece[,] board, Move move, bool isWhiteTurn)
        {
            if (board == null || move == null)
                return false;
            return board[move.From.X, move.From.Y].IsValidMove(move, board);
        }
        void applyingMoveOnBoard(Move move)
        {

            ChessPiece moveFrom = board[move.From.X, move.From.Y];
            bool isWhite = moveFrom.IsWhite;
            if (!board[move.Target.X, move.Target.Y].IsEmpty)
            {
                threeRepeatitiveBoardsReset();
                fiftyMovesTie = 0;
            }
            board[move.Target.X, move.Target.Y] = moveFrom;
            board[move.From.X, move.From.Y] = new ChessPiece();

            threeRepeatitiveBoardsSave(!isWhite);
            fiftyMovesTie++;
        }
        void moveExecution(Move move)
        {
            ChessPiece pieceToMove = board[move.From.X, move.From.Y];
            //king movement
            if (pieceToMove.ToString()[1] == 'K')
                kingMoved(move);
            //rook movement(checking for castling purposes)
            if (pieceToMove.ToString()[1] == 'R' &&
            ((move.From.X == 0 || move.From.X == 7) && (move.From.Y == 0 || move.From.Y == 0)))
                rookMoved(move);
            //pawn moved
            if (pieceToMove.ToString()[1] == 'P')
                pawnMoved(move);
            //applying changes on the board
            applyingMoveOnBoard(move);
        }
        ///////////////special - movements////////////////
        bool hasCastlingSucceded(bool isWhiteTurn)
        {
            if ((isWhiteTurn && hasWhiteKingMoved) || (!isWhiteTurn && hasBlackKingMoved)) { return false; }
            Console.WriteLine("Which rook would you like to use for castling?");
            string input = Console.ReadLine();
            string whichRook = input.ToUpper();
            switch (whichRook)
            {
                case "A1":
                    if (hasRookOnA1Moved || !isWhiteTurn) { return false; }
                    if (isCastleLegal(new Location(0, 0), isWhiteTurn))
                    { castleExecution(new Location(0, 0), isWhiteTurn, 2, 3); return true; }
                    return false;

                case "A8":
                    if (hasRookOnH1Moved || isWhiteTurn) { return false; }
                    if (isCastleLegal(new Location(7, 0), isWhiteTurn))
                    { castleExecution(new Location(7, 0), isWhiteTurn, 2, 3); return true; }
                    return false;
                case "H1":
                    if (hasRookOnA8Moved || !isWhiteTurn) { return false; }
                    if (isCastleLegal(new Location(0, 7), isWhiteTurn))
                    { castleExecution(new Location(0, 7), isWhiteTurn, 6, 5); return true; }
                    return false;
                case "H8":
                    if (hasRookOnH8Moved || isWhiteTurn) { return false; }
                    if (isCastleLegal(new Location(7, 7), isWhiteTurn))
                    { castleExecution(new Location(7, 7), isWhiteTurn, 6, 5); return true; }
                    return false;
                default:
                    return false;
            }
        }
        void castleExecution(Location rookLocation, bool isWhiteTurn, int colToMoveKingTo, int colToMoveRookTo)
        {
            Location kingLocation = findKing(board, isWhiteTurn);
            Location newKingLocation = new Location(kingLocation.X, colToMoveKingTo);
            Location newRookLocation = new Location(kingLocation.X, colToMoveRookTo);
            Move castleMoveRook = new Move(rookLocation, newRookLocation);
            Move castleMoveKing = new Move(kingLocation, newKingLocation);
            moveExecution(castleMoveKing);
            moveExecution(castleMoveRook);
        }
        bool isCastleLegal(Location rookLocation, bool isWhiteTurn)
        {
            if ((isWhiteTurn && hasWhiteKingMoved) || (!isWhiteTurn && hasBlackKingMoved))
                return false;
            Location kingLocation = findKing(board, isWhiteTurn);
            int start, end;
            if (rookLocation.Y > kingLocation.Y)
            {
                start = kingLocation.Y;
                end = rookLocation.Y;
            }
            else
            {
                start = rookLocation.Y;
                end = kingLocation.Y;
            }
            for (int i = start; i <= end; i++)
            {
                Location pathToCastling = new Location(kingLocation.X, i);
                if (isThreatend(board, pathToCastling, !isWhiteTurn))
                    return false;
            }
            return true;
        }
        void kingMoved(Move move)
        {
            if (board[move.From.X, move.From.Y].IsWhite)
            {
                if (!hasWhiteKingMoved)
                    threeRepeatitiveBoardsReset();
                hasWhiteKingMoved = true;
            }
            else
            {
                if (!hasBlackKingMoved)
                    threeRepeatitiveBoardsReset();
                hasBlackKingMoved = true;
            }
        }
        void rookMoved(Move move)
        {
            if (move.From.X == 0)
            {
                if (move.From.Y == 0 && !hasRookOnA1Moved)
                    hasRookOnA1Moved = true;
                else if (move.From.Y == 7 && !hasRookOnH1Moved)
                    hasRookOnH1Moved = true;
            }
            else if (move.From.X == 7)
            {
                if (move.From.Y == 0 && !hasRookOnA8Moved)
                    hasRookOnA8Moved = true;
                else if (move.From.Y == 7 && !hasRookOnH8Moved)
                    hasRookOnH8Moved = true;
            }
            else
                return;
            threeRepeatitiveBoardsReset();
        }
        void pawnMoved(Move move)
        {
            //promotion
            if (move.Target.X == 0 || move.Target.X == 7)
            {
                if (board[move.From.X, move.From.Y].IsWhite)
                    board[move.From.X, move.From.Y] = promotion(true);
                else
                    board[move.From.X, move.From.Y] = promotion(false);
            }
            //en passant capture
            if (board[move.From.X, move.Target.Y].IsPawnEnPassantThreat && board[move.Target.X, move.Target.Y].IsEmpty
                && move.Target.Y != move.From.Y)
                board[move.From.X, move.Target.Y] = new ChessPiece();
            threeRepeatitiveBoardsReset();
            fiftyMovesTie = 0;
        }
        ChessPiece promotion(bool isWhite)
        {
            Console.WriteLine("Enter what piece you would like to promote to (Q, B, N, R). [Q is default]");
            string input = Console.ReadLine();
            switch (input.ToUpper())
            {
                case "B":
                    return new Bishop(isWhite);
                case "N":
                    return new Knight(isWhite);
                case "R":
                    return new Rook(isWhite);
                default:
                    return new Queen(isWhite);
            }
        }
        void enPassantReset(bool isWhiteTurnOver)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (!board[i, j].IsEmpty)
                    {
                        if ((isWhiteTurnOver && !board[i, j].IsWhite || !isWhiteTurnOver && board[i, j].IsWhite) && board[i, j].IsPawnEnPassantThreat)
                            board[i, j].IsPawnEnPassantThreat = false;
                    }
                }
            }
        }
        /////////////////////Ties////////////////////////
        void threeRepeatitiveBoardsSave(bool isWhiteTurn)
        {
            savedBoards.Add(convertBoardToStringArray());
            isWhiteTurnOnSavedBoards.Add(isWhiteTurn);
        }
        void threeRepeatitiveBoardsReset()
        {
            savedBoards.Clear();
            isWhiteTurnOnSavedBoards.Clear();
        }
        bool isThreeRepeatitiveBoardsTie(bool isWhiteTurn)
        {
            int counter = 0;
            for (int i = 0; i < savedBoards.Count - 1; i++)
            {
                if ((string)savedBoards[i] == (string)savedBoards[savedBoards.Count - 1] && (bool)isWhiteTurnOnSavedBoards[i] == isWhiteTurn)
                    counter++;
            }
            if (counter >= 2)
            {
                isOverThreeRepeatitiveBoards = true;
                return true;
            }
            return false;
        }
        string convertBoardToStringArray()
        {
            string ret = "";
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    ret += board[i, j].ToString();
            return ret;
        }
        bool isNotEnoughPiecesTie()
        {
            int counterWhite = 0, counterBlack = 0;
            string stringBoard = convertBoardToStringArray();
            for (int i = 1; i < stringBoard.Length; i++)
            {
                char pieceType = stringBoard[i], pieceColor = stringBoard[i - 1];
                if (pieceType == 'Q' || pieceType == 'R' || pieceType == 'P') { return false; }
                else if ((pieceType == 'B' && (pieceColor == 'W' || pieceColor == 'B')) || pieceType == 'N')
                {
                    if (pieceColor == 'W')
                        counterWhite++;
                    else
                        counterBlack++;
                }
            }
            if (counterBlack + counterWhite > 1)
                return false;
            isOverNotEnoughPieces = true;
            return true;
        }
        bool isMutualAgreedTie()
        {
            Console.WriteLine("Would you like to accept mutually end the game in a tie?[Y]");
            try
            {
                string answer = Console.ReadLine();
                if (answer.ToUpper() == "Y")
                {
                    Console.WriteLine("Game has ended in a mutual agreed tie");
                    return true;
                }
            }
            catch (Exception) { return false; }
            return false;
        }
        bool HasGameEndedInATie(bool isWhiteTurn)
        {
            if (fiftyMovesTie == 50 || isThreeRepeatitiveBoardsTie(isWhiteTurn) || isMate(!isWhiteTurn) || isNotEnoughPiecesTie())
            {
                PrintBoard();
                Console.WriteLine("Game ended in a Tie!");
                if (fiftyMovesTie == 50)
                    Console.WriteLine("50 moves with no capture or pawn movement");
                else if (isOverThreeRepeatitiveBoards)
                    Console.WriteLine("same three boards repeated!");
                else if (isOverNotEnoughPieces)
                    Console.WriteLine("Not enough pieces on the board");
                else
                    Console.WriteLine("Stalemate! No legal moves available");
                return true;
            }
            return false;
        }
        ////////////////check, mate, game over//////////////////
        bool isGameOver(bool isWhiteTurn)
        {
            if (isCheck(board, !isWhiteTurn))
            {
                isActiveCheck = true;
                if (isMate(!isWhiteTurn))
                {
                    PrintBoard();
                    Console.WriteLine("Check!");
                    Console.WriteLine("Mate!" + (isWhiteTurn ? "Player 2[B]" : "Player 1[W]") + "has won!");
                    return true;
                }
                return false;
            }
            isActiveCheck = false;
            return HasGameEndedInATie(isWhiteTurn);
        }
        bool isThreatend(ChessPiece[,] board, Location target, bool isWhiteThreatning)
        {
            if (target != null && board != null)
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        Location from = new Location(i, j);
                        Move move = new Move(from, target);
                        if (!board[i, j].IsEmpty && isWhiteThreatning == board[i, j].IsWhite && board[i, j].IsValidMove(move, board))
                            return true;
                    }
                }
            return false;
        }
        bool isValidMoveNoCheck(ChessPiece[,] board, bool isWhiteThreatning)
        {
            if (board == null)
                return false;
            Location kingLocation = findKing(board, !isWhiteThreatning);
            return !isCheck(board, isWhiteThreatning);
        }
        bool isCheck(ChessPiece[,] board, bool isWhiteThreatning)
        {
            return isThreatend(board, findKing(board, !isWhiteThreatning), isWhiteThreatning);
        }
        bool isMate(bool isWhiteThreatning)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Location from = new Location(i, j);
                    if (!board[from.X, from.Y].IsEmpty && board[from.X, from.Y].IsWhite == !isWhiteThreatning)
                    {
                        for (int k = 0; k < 8; k++)
                        {
                            for (int l = 0; l < 8; l++)
                            {
                                Location target = new Location(k, l);
                                Move move = new Move(from, target);
                                ChessPiece[,] hypotheticBoard = applyingMoveOnHypotheticBoardIfLegal(board, move, isWhiteThreatning);
                                Location kingLocation = findKing(hypotheticBoard, !isWhiteThreatning);
                                if (isValidMoveNoCheck(hypotheticBoard, isWhiteThreatning))
                                    return false;
                            }
                        }
                    }
                }
            }
            return true;
        }
        Location findKing(ChessPiece[,] board, bool isWhite)
        {
            if (board != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        if (isWhite && board[i, j].ToString() == "WK")
                            return new Location(i, j);
                        if (!isWhite && board[i, j].ToString() == "BK")
                            return new Location(i, j);
                    }
                }
            }
            return null;
        }
        ChessPiece[,] applyingMoveOnHypotheticBoardIfLegal(ChessPiece[,] board, Move move, bool isWhiteThreatning)
        {
            ChessPiece[,] hypotheticBoard = createHypotheticBoard(board);
            if (isLegalMove(hypotheticBoard, move, !isWhiteThreatning))
            {
                hypotheticBoard[move.Target.X, move.Target.Y] = board[move.From.X, move.From.Y];
                hypotheticBoard[move.From.X, move.From.Y] = new ChessPiece();
            }
            else
                hypotheticBoard = null;
            return hypotheticBoard;
        }
        ChessPiece[,] createHypotheticBoard(ChessPiece[,] board)
        {
            ChessPiece[,] hypotheticBoard = new ChessPiece[8, 8];
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    hypotheticBoard[i, j] = board[i, j];
            return hypotheticBoard;
        }
    }
    class Location
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public Location(int x, int y)
        {
            X = x;
            Y = y;
        }
        public Location() { }
    }
    class Move
    {
        public Location From { get; private set; }
        public Location Target { get; private set; }
        public Move(Location from, Location target)
        {
            From = from;
            Target = target;
        }
        public Move() { }
    }
    class ChessPiece
    {
        public ChessPiece() { IsEmpty = true; }
        public bool IsWhite { get; set; }
        public bool IsEmpty { get; set; }
        public bool IsPawnEnPassantThreat { get; set; }
        public override string ToString()
        {
            return "EE";
        }
        public virtual bool IsValidMove(Move move, ChessPiece[,] board) { return false; }
    }
    class King : ChessPiece
    {
        public King(bool isWhite)
        {
            IsWhite = isWhite;
            IsEmpty = false;
        }

        public override string ToString()
        {
            return IsWhite ? "WK" : "BK";
        }
        public override bool IsValidMove(Move move, ChessPiece[,] board)
        {
            Queen allMovements = new Queen(IsWhite);
            if (Math.Abs(move.From.X - move.Target.X) > 1 || Math.Abs(move.From.Y - move.Target.Y) > 1)
                return false;
            return allMovements.IsValidMove(move, board);
        }

    }
    class Queen : ChessPiece
    {
        public Queen(bool isWhite)
        {
            IsWhite = isWhite;
            IsEmpty = false;
        }

        public override string ToString()
        {
            return IsWhite ? "WQ" : "BQ";
        }
        public override bool IsValidMove(Move move, ChessPiece[,] board)
        {
            Bishop diagonalMovement = new Bishop(IsWhite);
            Rook straightMovement = new Rook(IsWhite);
            if (diagonalMovement.IsValidMove(move, board) || straightMovement.IsValidMove(move, board))
                return true;
            return false;
        }

    }
    class Bishop : ChessPiece
    {
        public Bishop(bool isWhite)
        {
            IsWhite = isWhite;
            IsEmpty = false;
        }

        public override string ToString()
        {
            return IsWhite ? "WB" : "BB";
        }
        public override bool IsValidMove(Move move, ChessPiece[,] board)
        {
            if (move != null && board != null && (Math.Abs(move.From.X - move.Target.X) == Math.Abs(move.From.Y - move.Target.Y)))
            {
                //left-up
                if (move.From.X > move.Target.X && move.From.Y > move.Target.Y)
                    return isValidDiagonalUpLeftMovement(move, board);
                //right-up
                else if (move.From.X > move.Target.X && move.From.Y < move.Target.Y)
                    return isValidDiagonalUpRightMovement(move, board);
                //left-down
                else if (move.From.X < move.Target.X && move.From.Y > move.Target.Y)
                    return isValidDiagonalDownLeftMovement(move, board);
                //right-down
                else if (move.From.X < move.Target.X && move.From.Y < move.Target.Y)
                    return isValidDiagonalDownRightMovement(move, board);
            }
            return false;
        }
        bool isValidDiagonalMovement(Move move, ChessPiece[,] board, int i, int j)
        {
            if (i == move.Target.X && j == move.Target.Y)
            {
                if (board[i, j].IsWhite == IsWhite && !board[i, j].IsEmpty)
                    return false;
            }
            else if (!board[i, j].IsEmpty)
                return false;
            return true;
        }
        bool isValidDiagonalUpLeftMovement(Move move, ChessPiece[,] board)
        {
            for (int i = move.From.X - 1, j = move.From.Y - 1; i >= move.Target.X && j >= move.Target.Y; i--, j--)
                if (!isValidDiagonalMovement(move, board, i, j))
                    return false;
            return true;
        }
        bool isValidDiagonalDownLeftMovement(Move move, ChessPiece[,] board)
        {
            for (int i = move.From.X + 1, j = move.From.Y - 1; i <= move.Target.X && j >= move.Target.Y; i++, j--)
                if (!isValidDiagonalMovement(move, board, i, j))
                    return false;
            return true;
        }
        bool isValidDiagonalUpRightMovement(Move move, ChessPiece[,] board)
        {
            for (int i = move.From.X - 1, j = move.From.Y + 1; i >= move.Target.X && j <= move.Target.Y; i--, j++)
                if (!isValidDiagonalMovement(move, board, i, j))
                    return false;
            return true;
        }
        bool isValidDiagonalDownRightMovement(Move move, ChessPiece[,] board)
        {
            for (int i = move.From.X + 1, j = move.From.Y + 1; i <= move.Target.X && j <= move.Target.Y; i++, j++)
                if (!isValidDiagonalMovement(move, board, i, j))
                    return false;
            return true;
        }
    }
    class Knight : ChessPiece
    {
        public Knight(bool isWhite)
        {
            IsWhite = isWhite;
            IsEmpty = false;
        }

        public override string ToString()
        {
            return IsWhite ? "WN" : "BN";
        }
        public override bool IsValidMove(Move move, ChessPiece[,] board)
        {
            if (IsLegalKnightMovement(move.From, move.Target))
                if ((board[move.Target.X, move.Target.Y].IsWhite == !IsWhite) ||
                board[move.Target.X, move.Target.Y].IsEmpty)
                    return true;
            return false;
        }
        public bool IsLegalKnightMovement(Location from, Location target)
        {
            if ((from.X == target.X + 1 && from.Y == target.Y + 2) || (from.X == target.X - 1 && from.Y == target.Y + 2) ||
            (from.X == target.X + 1 && from.Y == target.Y - 2) || (from.X == target.X - 1 && from.Y == target.Y - 2) ||
            (from.X == target.X + 2 && from.Y == target.Y + 1) || (from.X == target.X - 2 && from.Y == target.Y + 1) ||
            (from.X == target.X + 2 && from.Y == target.Y - 1) || (from.X == target.X - 2 && from.Y == target.Y - 1))
                return true;
            return false;
        }
    }
    class Rook : ChessPiece
    {
        public Rook(bool isWhite)
        {
            IsWhite = isWhite;
            IsEmpty = false;
        }

        public override string ToString()
        {
            return IsWhite ? "WR" : "BR";
        }
        public override bool IsValidMove(Move move, ChessPiece[,] board)
        {
            //vertical
            if (move.From.X == move.Target.X)
            {
                if (move.From.Y > move.Target.Y)//left
                    return leftMovement(move, board);
                else if (move.From.Y < move.Target.Y)//right
                    return rightMovement(move, board);
            }
            //portrait
            if (move.From.Y == move.Target.Y)
            {
                if (move.From.X > move.Target.X)//up
                    return upMovement(move, board);
                else if (move.From.X < move.Target.X)//down
                    return downMovement(move, board);
            }
            return false;
        }
        bool isValidMovement(Move move, ChessPiece[,] board, int i, int j)
        {
            if (i == move.Target.X && j == move.Target.Y)
            {
                if (board[i, j].IsWhite == IsWhite && !board[i, j].IsEmpty)
                    return false;
            }
            else if (!board[i, j].IsEmpty)
                return false;
            return true;
        }
        bool upMovement(Move move, ChessPiece[,] board)
        {
            for (int i = move.From.X - 1; i >= move.Target.X; i--)
            {
                if (!isValidMovement(move, board, i, move.From.Y))
                    return false;
            }
            return true;
        }
        bool downMovement(Move move, ChessPiece[,] board)
        {
            for (int i = move.From.X + 1; i <= move.Target.X; i++)
            {
                if (!isValidMovement(move, board, i, move.From.Y))
                    return false;
            }
            return true;
        }
        bool leftMovement(Move move, ChessPiece[,] board)
        {
            for (int i = move.From.Y - 1; i >= move.Target.Y; i--)
            {
                if (!isValidMovement(move, board, move.From.X, i))
                    return false;
            }
            return true;
        }
        bool rightMovement(Move move, ChessPiece[,] board)
        {
            for (int i = move.From.Y + 1; i <= move.Target.Y; i++)
            {
                if (!isValidMovement(move, board, move.From.X, i))
                    return false;
            }
            return true;
        }
    }
    abstract class Pawn : ChessPiece
    {
        public bool IsValidMove(Location from, Location target, ChessPiece[,] board, bool isWhiteTurn)
        {
            int upOrDownPawnDirection = IsWhite ? -1 : 1;
            //one step forward
            if (board[target.X, target.Y].IsEmpty && from.Y == target.Y && from.X == target.X + upOrDownPawnDirection)
                return true;
            //two steps forward
            else if (board[target.X, target.Y].IsEmpty && from.Y == target.Y &&
            from.X == target.X + 2 * upOrDownPawnDirection && (isWhiteTurn ? from.X == 1 : from.X == 6))
            {
                IsPawnEnPassantThreat = true;
                return true;
            }
            //capturing
            else if (!board[target.X, target.Y].IsEmpty && (board[target.X, target.Y].IsWhite == !isWhiteTurn) &&
                    from.X == target.X + upOrDownPawnDirection && (from.Y == target.Y + 1 || from.Y == target.Y - 1))
                return true;
            //en passant
            else if (board[from.X, target.Y].IsPawnEnPassantThreat && (from.Y == target.Y + 1 || from.Y == target.Y - 1) &&
                    from.X == target.X + upOrDownPawnDirection && ((isWhiteTurn && from.X == 4) || (!isWhiteTurn && from.X == 3)))
                return true;
            return false;
        }
    }
    class WhitePawn : Pawn
    {
        public WhitePawn()
        {
            IsWhite = true;
            IsEmpty = false;
        }

        public override string ToString() { return "WP"; }
        public override bool IsValidMove(Move move, ChessPiece[,] board)
        {
            return base.IsValidMove(move.From, move.Target, board, true);
        }
    }
    class BlackPawn : Pawn
    {
        public BlackPawn()
        {
            IsWhite = false;
            IsEmpty = false;
        }

        public override string ToString() { return "BP"; }
        public override bool IsValidMove(Move move, ChessPiece[,] board)
        {
            return base.IsValidMove(move.From, move.Target, board, false);
        }
    }
    ////////Exception
    class TieRequestException : Exception { }
    class CastleException : Exception { }
    class IllegalMoveException : Exception { }
    class CheckException : Exception { }
}
