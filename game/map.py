from character import Character
from game.game import Game
from game.exceptions import *
from game.events import MovementHit

from enum import Enum
# import copy

class direction(Enum):
    UP    = 1
    RIGHT = 2
    DOWN  = 3
    LEFT  = 4
    def __mul__(self,rhs):
        '''
        return the inner product of two directions
        '''
        diff = self.value - rhs.value
        if diff == 0:
            return 1
        if diff == 1 or diff == -1 or diff == 3 or diff == -3:
            return 0
        if diff == 2 or diff == -2:
            return -1

class Map:
    chara_to_location = {}  # Character的实例映射到地图上的点(x,y)
    # location_to_chara  # 地图(x,y)映射到该点上的Character实例

    def __init__(self, game:Game, x_size = 12, y_size = 11):
        self.game = game
        self.location_to_chara = [[None for _ in range(x_size)] for _ in range(y_size)]
        self.x_size = x_size
        self.y_size = y_size
        # go right to increase x and go down to increase y
        '''x==5; y==3
       y\\x 0    1    2   3    4
        0   □   □   □   □   □
        1   □   □   □   □   □
        2   □   □   □   □   □
        '''

    def find_nearest_enemy(self, character:Character):
        '''find the nearest enemy character for the input character'''
        character_team = self.game.character_to_team[character]
        min_dist = float('inf')
        target = None
        for team in self.game.team_to_character:
            if team == character_team:
                pass
            else:
                for enemy_character in self.game.team_to_character[team].characters:
                    dist = self.compute_distance(character, enemy_character)
                    if dist < min_dist:
                        min_dist = dist
                        target = enemy_character
        if not target:
            raise NoTargetFound
        return target

    def compute_distance(self, c1, c2):
        l1, l2 = self.chara_to_location[c1], self.chara_to_location[c2]
        return abs(l1[0]-l2[0])+abs(l1[1]-l2[1])

    def new_character(self, character:Character, x, y):
        if self.location_to_chara[y][x]:
            raise LocationConflict(self.location_to_chara, character, x,y)
        self.chara_to_location[character] = (x,y)

    def move(self, character):
        instruction = character.movement_instruction
        if instruction:
            dir, dist = instruction.dir, instruction.dist
            current_location = self.chara_to_location[character]
            # 先假设不会撞到其他角色。移动并检测地图边缘
            if dir == direction.UP:
                covered_location = [(current_location[0], current_location[1]-i-1) if current_location[1]-i-1>0 else 0 for i in range(dist)]
            elif dir == direction.RIGHT:
                covered_location = [(current_location[0]+i+1, current_location[1]) if current_location[0]+i+1<self.x_size else self.x_size-1 for i in range(dist)]
            elif dir == direction.DOWN:
                covered_location = [(current_location[0], current_location[1]+i+1) if current_location[1]+i+1<self.y_size else self.y_size-1 for i in range(dist)]
            elif dir == direction.LEFT:
                covered_location = [(current_location[0]-i-1, current_location[1]) if current_location[0]-i-1>0 else 0 for i in range(dist)]
            final_location = current_location
            # 在通过的路线上检测是否碰到过其他角色
            movement_hit_event = None
            for location in covered_location:
                chara_on_map = self.location_to_chara[location[1]][location[0]]
                if chara_on_map:
                    movement_hit_event = MovementHit(character, chara_on_map)
                    break
                else:
                    final_location = location
            # 设定最终到达的地点
            self.chara_to_location[character] = final_location
            self.location_to_chara[current_location[1]][current_location[0]] = None
            self.location_to_chara[final_location[1]][final_location[0]] = character
            return movement_hit_event

    def map_to_str(self):
        # map = copy.deepcopy(self.location_to_chara)
        return '\n'.join([''.join([character.name[0] if character else '□' for character in line]) for line in self.location_to_chara])