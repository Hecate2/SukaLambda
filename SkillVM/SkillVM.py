from abc import ABC, abstractmethod
from queue import SimpleQueue
from typing import Set


# SkillVM == [
#     [SkillExecution, SkillExecution, ...], # coming Round
#     [SkillExecution, SkillExecution, ...], # future Rounds...
# ]

class Map:
    def __init__(self):
        pass


class ICharacter(ABC):
    def __init__(self):
        pass


class ISkill(ABC):
    def __init__(self, owner: ICharacter):
        self.owner = owner
    
    @abstractmethod
    def generate_execution(self):  # return ISkillExecution
        pass


class ISkillVM(ABC):
    def __init__(self, characters: Set[ICharacter], map: Map):
        pass
    
    @abstractmethod
    def prepare_skill(self, skill: ISkill):
        pass
    
    @abstractmethod
    def execute_round(self):
        pass


class ISkillExecution(ABC):
    def __init__(self, skill: ISkill, targets: Set[ICharacter]):
        self.skill = skill
        self.target = targets
    
    @abstractmethod
    def execute(self, vm: ISkillVM):
        pass


class Round(SimpleQueue[ISkillExecution]):
    def execute(self, vm: ISkillVM):
        while self.qsize() > 0:
            ske = self.get()
            ske.execute(vm)


class SkillVM(ISkillVM):
    def __init__(self, characters: Set[ICharacter], map_: Map):
        super().__init__(characters, map_)
        self.characters = characters
        self.map_ = map_
