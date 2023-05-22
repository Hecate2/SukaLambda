from SkillVM.skill import Skill
from SkillVM.character import Character

class Event:
    pass

class RoundFinish(Event):
    pass

class MyNewRound(RoundFinish):
    pass
my_new_round_event = MyNewRound()

class MyRoundFinish(RoundFinish):
    '''
    I have finished my round
    '''
    pass
my_round_finish_event = MyRoundFinish()

class GameRoundFinish(RoundFinish):
    '''
    Everyone has finished one round
    '''
    pass
game_round_finish_event = GameRoundFinish()

class MyMovementFinish(RoundFinish):
    pass
my_movement_finish_event = MyMovementFinish()

class SkillHit(Event):
    def __init__(self, source_skill:Skill, target:Character):
        '''
        source_skill has hit target
        '''
        self.source_skill = source_skill
        self.target = target

class MovementHit(Event):
    def __init__(self, source:Character, target:Character):
        '''
        someone hit another by movement
        '''
        self.source = source
        self.target = target

class MovementHitBy(Event):
    def __init__(self, source:Character, target:Character):
        '''
        someone hit another by movement
        '''
        self.source = source
        self.target = target

class CharacterFade(Event):
    def __init__(self, charcter:Character):
        self.character = charcter