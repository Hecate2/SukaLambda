from game.map import Map
from game.team import Team
from game.events import *


class Game:
    characters = []
    character_to_team={}
    str_to_print = []

    def __init__(self, teams=2):
        self.map = Map(self)
        self.team_to_character = [Team() for _ in range(teams)]

    def add_character(self, character:Character, team:int=0):
        self.characters.append(character)
        self.team_to_character[team].add_character(character)
        self.character_to_team[character] = team

    def remove_character(self, character:Character):
        self.characters.remove(character)
        team = self.character_to_team[character]
        self.team_to_character[team].remove_character(character)
        self.character_to_team.pop(character)

    def register_print(self, str_to_print):
        self.str_to_print.append(str_to_print)

    def execute_one_round(self):
        self.characters.sort(key=lambda character:character.agi, reverse=True)
        for character in self.character_to_team:
            character.handle_event(my_new_round_event)
            movement_hit_event = self.map.move(character)
            if movement_hit_event:
                character.handle_event(movement_hit_event)
                movement_hit_event.target.handle_event(movement_hit_event)
            character.handle_event(my_movement_finish_event)
        for character in self.characters:
            target:Character = self.map.find_nearest_enemy(character)
            target.handle_event(SkillHit(character.skill_instruction, target))
            character.handle_event(my_round_finish_event)
        for character in self.characters:
            character.handle_event(game_round_finish_event)
