from game.map import direction
from character import Character

class MovementInstruction:
    def __init__(self, dir:direction, dist:int):
        self.dir = dir
        self.dist = dist