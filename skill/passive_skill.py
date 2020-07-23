from character import Character
from skill.skill import Skill
from skill.effect import Effect
from game import *

class PassiveSkill:
    def __init__(self, owner:Character):
        self.owner = owner
    def __call__(self, origin_skill:Skill, effect:Effect) -> Effect:
        # This is just an example which does nothing:
        # effect.damage = effect.damage
        # effect.mp_damage = effect.mp_damage
        # effect.meta_effects = effect.meta_effects
        # You can inherit class PassiveSkill and implement your own logics. For example, effect.damage /= 2
        return effect  # pass


class PassiveSkillInstructionList:
    def __init__(self, owner:Character):
        self.owner = owner
        self.instruction_list = [[]]  # [[passive, skills, for, round, 0], [passive, skills, for, round, 1]]

    def add_passive_skill(self, passive_skill:PassiveSkill, rounds:list[int]):
        '''
        activate the passive_skill in the following rounds
        e.g. add_passive_skill(parry, [0,2]) activates parry in this round, and the round after the next
        :param passive_skill:
        :param rounds:
        :return:
        '''
        len_instruction_list = len(self.instruction_list)
        while max(rounds) <= len_instruction_list:
            self.instruction_list.append([])
        for round_index in rounds:
            self.instruction_list[round_index].append(passive_skill)

    def new_round(self):
        '''
        when my new round comes, call this method to delete the passive skills for the previous round,
        and activate the passive skills for the new round
        '''
        try:
            self.instruction_list.pop(0)
        except IndexError:
            pass

    def get_current_passive_skills(self):
        try:
            return self.instruction_list[0]
        except IndexError:
            return []

    def __getitem__(self, index:int):
        # not recommended to use
        try:
            return self.instruction_list[index]
        except IndexError:
            return []

    def __setitem__(self, key:int, value:PassiveSkill):
        # not recommended to use
        self.add_passive_skill(value, [key])
