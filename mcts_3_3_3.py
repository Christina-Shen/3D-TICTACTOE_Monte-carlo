import math
import random
import numpy as np
from itertools import permutations, product
from typing import Dict, Tuple, List, Callable

# 棋盤狀態的資料結構
class GameState:
    def __init__(self, position: Dict, player_1: str, player_2: str):
        self.position = position  # Dict[Tuple[int,int,int], str]
        self.player_1 = player_1
        self.player_2 = player_2

    def copy(self):
        return GameState(
            self.position.copy(),
            self.player_1,
            self.player_2
        )

# 樹節點定義
class TreeNode:
    def __init__(self, state: GameState, parent, is_terminal: bool):
        self.state = state
        self.is_terminal = is_terminal
        self.is_fully_expanded = is_terminal
        self.parent = parent
        self.visits = 0
        self.score = 0
        self.children = {}

# MCTS 改成傳參數版本
class MCTS_3d_Parameterized:
    def __init__(self,
                 is_win_func: Callable[[GameState], bool],
                 is_draw_func: Callable[[GameState], bool],
                 generate_states_func: Callable[[GameState], List[GameState]]):
        """
        初始化 MCTS，傳入遊戲規則的函數

        Args:
            is_win_func: 判斷是否有玩家獲勝 (檢查 player_2 是否贏)
            is_draw_func: 判斷是否平局
            generate_states_func: 生成所有可能的下一步狀態
        """
        self.is_win = is_win_func
        self.is_draw = is_draw_func
        self.generate_states = generate_states_func

    def search(self, initial_state: GameState, iterations=10000, verbose=False):
        # 判斷初始狀態是否為終止狀態
        is_terminal = self.is_win(initial_state) or self.is_draw(initial_state)
        self.root = TreeNode(initial_state, None, is_terminal)

        for iteration in range(iterations):
            node = self.select(self.root)
            score = self.rollout(node.state)
            self.backpropagate(node, score)

            if verbose and iteration % 1000 == 0:
                print(f"Iteration {iteration}: Root visits={self.root.visits}, children={len(self.root.children)}")

        best_node = self.get_best_move(self.root, 0)

        # 找出最佳移動的座標 (depth, row, col)
        if best_node is None:
            return None

        return self.get_move_coords(initial_state, best_node.state)

    def get_move_coords(self, old_state: GameState, new_state: GameState):
        """比較兩個狀態，找出差異的座標"""
        for d in range(3):
            for r in range(3):
                for c in range(3):
                    if old_state.position[(d, r, c)] != new_state.position[(d, r, c)]:
                        return (d, r, c)
        return None

    def select(self, node):
        while not node.is_terminal:
            if node.is_fully_expanded:
                node = self.get_best_move(node, math.sqrt(2))
            else:
                return self.expand(node)
        return node

    def expand(self, node):
        states = self.generate_states(node.state)
        if not states:
            node.is_fully_expanded = True
            return node

        for state in states:
            state_id = self._state_id(state)
            if state_id not in node.children:
                is_terminal = self.is_win(state) or self.is_draw(state)
                new_node = TreeNode(state, node, is_terminal)
                node.children[state_id] = new_node

                if len(node.children) == len(states):
                    node.is_fully_expanded = True

                return new_node

        node.is_fully_expanded = True
        return node

    def rollout(self, state: GameState):
        current_state = state.copy()

        while not self.is_win(current_state):
            try:
                next_states = self.generate_states(current_state)
                if not next_states:
                    return 0
                current_state = random.choice(next_states)
            except:
                return 0

        # 返回分數 (從 'x' 的角度)
        if current_state.player_2 == 'x':
            return 10
        elif current_state.player_2 == 'o':
            return -10
        return 0

    def backpropagate(self, node, score):
        while node is not None:
            node.visits += 1
            node.score += score
            node = node.parent

    def get_best_move(self, node, exploration_constant):
        if not node.children:
            return None

        unvisited = [ch for ch in node.children.values() if ch.visits == 0]
        if unvisited:
            return random.choice(unvisited)

        best_child = None
        best_value = float('-inf')

        total_visits = sum(child.visits for child in node.children.values())
        sign = 1 if node.state.player_1 == 'x' else -1

        for child in node.children.values():
            if child.visits == 0:
                continue

            avg_reward = child.score / child.visits
            exploration = exploration_constant * math.sqrt(
                math.log(total_visits) / child.visits
            )

            ucb_value = sign * avg_reward + exploration

            if ucb_value > best_value:
                best_value = ucb_value
                best_child = child

        return best_child

    # Canonical ID 相關方法
    def board_to_array(self, state: GameState):
        n = 3
        A = np.empty((n, n, n), dtype='U1')
        for d in range(n):
            for r in range(n):
                for c in range(n):
                    A[d, r, c] = state.position[(d, r, c)]
        return A

    def serialize(self, A):
        return ''.join(A.reshape(-1, order='C'))

    def canonical_id(self, state: GameState):
        A = self.board_to_array(state)
        best = None
        axes = (0, 1, 2)

        for p in permutations(axes):
            B = A.transpose(p)
            for flips in product([False, True], repeat=3):
                C = B
                if flips[0]: C = C[::-1, :, :]
                if flips[1]: C = C[:, ::-1, :]
                if flips[2]: C = C[:, :, ::-1]
                s = self.serialize(C)
                if (best is None) or (s < best):
                    best = s
        return best

    def _state_id(self, state: GameState):
        return self.canonical_id(state)


# ============= 整合 Board_3d 的函數 =============

DIRS_3D = [
    (1,0,0), (0,1,0), (0,0,1),
    (1,1,0), (1,-1,0),
    (1,0,1), (1,0,-1),
    (0,1,1), (0,1,-1),
    (1,1,1), (1,1,-1), (1,-1,1), (1,-1,-1)
]

def board3d_is_win(state: GameState) -> bool:
    """檢查 player_2 是否獲勝（來自 Board_3d.is_win）"""
    n = 3
    pos = state.position
    who = state.player_2

    for z in range(n):
        for y in range(n):
            for x in range(n):
                if pos[z, y, x] != who:
                    continue
                for dx, dy, dz in DIRS_3D:
                    x_end = x + (n-1)*dx
                    y_end = y + (n-1)*dy
                    z_end = z + (n-1)*dz
                    if not (0 <= x_end < n and 0 <= y_end < n and 0 <= z_end < n):
                        continue
                    if all(pos[z + k*dz, y + k*dy, x + k*dx] == who for k in range(n)):
                        return True
    return False

def board3d_is_draw(state: GameState) -> bool:
    """檢查是否平局（來自 Board_3d.is_draw）"""
    for v in state.position.values():
        if v == '.':
            return False
    return True

def board3d_generate_states(state: GameState) -> List[GameState]:
    """生成所有可能的下一步狀態（來自 Board_3d.generate_states）"""
    if board3d_is_win(state) or board3d_is_draw(state):
        return []

    actions = []
    empty_square = '.'

    for dp in range(3):
        for row in range(3):
            for col in range(3):
                if state.position[dp, row, col] == empty_square:
                    # 建立新狀態
                    new_position = state.position.copy()
                    new_position[dp, row, col] = state.player_1

                    # 交換玩家
                    new_state = GameState(
                        new_position,
                        state.player_2,  # 新的 player_1
                        state.player_1   # 新的 player_2
                    )
                    actions.append(new_state)

    return actions


# ============= 輔助函數：Board_3d 與 GameState 轉換 =============

def board_to_gamestate(board) -> GameState:
    """將 Board_3d 物件轉換為 GameState"""
    return GameState(
        board.position.copy(),
        board.player_1,
        board.player_2
    )

def gamestate_to_board(state: GameState):
    """將 GameState 轉換回 Board_3d 物件"""
    from copy import deepcopy
    board = Board_3d()
    board.position = state.position.copy()
    board.player_1 = state.player_1
    board.player_2 = state.player_2
    return board
from copy import deepcopy
DIRS_3D = [
    (1,0,0),   # x 軸
    (0,1,0),   # y 軸
    (0,0,1),   # z 軸
    (1,1,0), (1,-1,0),   # xy 平面對角線
    (1,0,1), (1,0,-1),   # xz 平面對角線
    (0,1,1), (0,1,-1),   # yz 平面對角線
    (1,1,1), (1,1,-1), (1,-1,1), (1,-1,-1)   # 空間對角線
]
# Tic Tac Toe board class
from itertools import product
class Board_3d():
    # create constructor (init board class instance)
    ALL_KEYS = tuple(product(range(3), range(3), range(3)))
    def __init__(self, board=None):
        # define players
        self.player_1 = 'x' # User
        self.player_2 = 'o' # Compter
        self.empty_square = '.'

        # define board position
        self.position = {}
        self._keys = Board_3d.ALL_KEYS
        # init (reset) board
        self.init_board()

        # create a copy of a previous board state if available
        if board is not None:
            self.__dict__ = deepcopy(board.__dict__)

    # init (reset) board
    def init_board(self):
        # loop over board rows
        for depth in range(3):
            for row in range(3):
                # loop over board columns
                for col in range(3):
                    # set every board square to empty square
                    self.position[depth,row, col] = self.empty_square

    # make move
    def clone(self):
      b = Board_3d.__new__(Board_3d)  # 不呼叫 __init__
      b.player_1 = self.player_1
      b.player_2 = self.player_2
      b.empty_square = self.empty_square
      # 只複製 dict（淺拷貝就夠了，值是字元）
      b.position = self.position.copy()
      b._keys = self._keys
      return b
    def make_move(self,depth, row, col):
        # create new board instance that inherits from the current state

        ## board = Board(self) 是為了讓 make_move 建立一個新棋盤狀態，保持原來的棋盤不被改動，這樣才符合 MCTS 搜尋的需求。
        board =self.clone()

        # make move
        board.position[depth,row, col] = self.player_1

        # swap players
        (board.player_1, board.player_2) = (board.player_2, board.player_1)

        # return new board state
        return board

    # get whether the game is drawn
    def is_draw(self):
        # loop over board squares
        for v in self.position.values():
          if v == self.empty_square:
              return False

        # by default we return a draw
        return True
    def _in_bounds(self,d, r, c, n=3):
      return 0 <= d < n and 0 <= r < n and 0 <= c < n

    # get whether the game is won
    def is_win(self, player=None):
        """
        回傳是否有人在 3x3x3 棋盤連成一線（長度=3）。
        預設以 self.player_2 作為要檢查的棋子；也可傳入 player 覆蓋。
        """
        n = 3
        pos = self.position  # 形狀: (depth=z, row=y, col=x)
        who = self.player_2 if player is None else player

        for z in range(n):
            for y in range(n):
                for x in range(n):
                    if pos[z, y, x] != who:
                        continue
                    for dx, dy, dz in DIRS_3D:
                        x_end = x + (n-1)*dx
                        y_end = y + (n-1)*dy
                        z_end = z + (n-1)*dz
                        if not (0 <= x_end < n and 0 <= y_end < n and 0 <= z_end < n):
                            continue
                        if all(pos[z + k*dz, y + k*dy, x + k*dx] == who for k in range(n)):
                            return True
        return False



    # generate legal moves to play in the current position
    def generate_states(self):
        # define states list (move list - list of available actions to consider)
        if self.is_win() or self.is_draw():
          return []
        actions = []

        # loop over board rows
        for dp in range(3):
            for row in range(3):
                # loop over board columns
                for col in range(3):
                    # make sure that current square is empty
                    if self.position[dp,row, col] == self.empty_square:
                        # append available action/board state to action list
                        actions.append(self.make_move(dp,row, col))

        # return the list of available actions (board class instances)
        return actions

    # main game loop
    def game_loop(self):
        print('\n  Tic Tac Toe by Code Monkey King\n')
        print('  Type "exit" to quit the game')
        print('  Move format [depth,row,column]')

        # print board
        print(self)

        # create MCTS instance
        mcts = MCTS_3d_Parameterized(
            is_win_func=board3d_is_win,
            is_draw_func=board3d_is_draw,
            generate_states_func=board3d_generate_states
        )

        # game loop
        while True:
            # get user input
            user_input = input('> ')

            # escape condition
            if user_input == 'exit': break

            # skip empty input
            if user_input == '': continue

           # try:
              # parse user input (move format [depth,col, row]: 1,2)
            depth=int(user_input.split(',')[0])-1
            row = int(user_input.split(',')[1]) - 1
            col = int(user_input.split(',')[2]) - 1
            #print(['self.poistion[depth,row,col]=',self.position[depth,row,col]])
            # check move legality
            if self.position[depth,row, col] != self.empty_square:
                print(' Illegal move!')
                continue

            # make move on board
            self = self.make_move(depth,row, col)  ## user input r,c ->'x' ,player 1= 'o', player 2='x'

            # print board
            print(self)

            # search for the best move
            #GameState
            # best_move = mcts.search(self)# initial states : player 1= 'o', player 2='x' ,player 1 represents now turn is who , player 2 represnts  past term

            # # legal moves available
            # try:
            #     # make AI move here
            #     self = best_move.board

            # # game over
            # except:
            #     pass
            current_state = board_to_gamestate(self)
            best_move_coords = mcts.search(current_state, iterations=5000)

            if best_move_coords:
                d, r, c = best_move_coords
                self = self.make_move(d, r, c)
            # print board
            print(self) # 當你 print(物件) 的時候，會呼叫這個物件的 __str__ 方法。

            # check if the game is won
            if self.is_win():
                print('player "%s" has won the game!\n' % self.player_2)
                break

            # check if the game is drawn
            elif self.is_draw():
                print('Game is drawn!\n')
                break
    # print board state
    def __str__(self):
        # define board string representation
        board_string = ''

        # loop over board rows
        for dp in range(3):
            board_string+='depth=%s\n' %dp
            for row in range(3):
                # loop over board columns
                for col in range(3):
                    board_string += ' %s' % self.position[dp,row, col]

                # print new line every row
                board_string += '\n'

        # prepend side to move
        if self.player_1 == 'x':
            board_string = '\n--------------\n "x" to move:\n--------------\n\n' + board_string

        elif self.player_1 == 'o':
            board_string = '\n--------------\n "o" to move:\n--------------\n\n' + board_string

        # return board string
        return board_string

# main driver
if __name__ == '__main__':
    # create board instance
    board = Board_3d()

    # start game loop
    board.game_loop()