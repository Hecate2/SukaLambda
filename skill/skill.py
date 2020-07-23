from character.beast import Beast
from character.character import Character
from character.ship import Ship

from game import Game
from skill.effect import Effect

import numpy as np

class Skill:
    class SkillEffect(Effect):
        pass
    def __init__(self, name='示例技能名', description='示例技能效果说明', owner:Character=None):
        self.name = name
        self.description = description
        self.mp_cost = 0
        self.set_owner(owner)
    def __call__(self, game:Game, target: Beast or Character or Ship=None):
        # What happens when the skill is actually executed
        # usage: sk = skill(target); sk(global_game_context)
        # fell free to define your own logics in self.SkillEffect
        return self.SkillEffect(self)
    def set_owner(self, owner: Character or Beast):
        self.owner = owner
        owner.skills.append(self)
    @property
    def info(self):
        return f'{self.name}: {self.description}\nATK:[攻击力]  MP_COST: [魔力消耗]'

class MoveHitSkill(Skill):
    def __init__(self, hp_damage, name='冲撞', description='通过移动来冲撞对手', owner: Character = None):
        super().__init__(name, description, owner)
        self.skill_effect = self.SkillEffect(self, hp_damage=hp_damage)
    class SkillEffect(Effect):
        pass
    def __call__(self, game:Game, target: Beast or Character or Ship=None):
        # What happens when the skill is actually executed
        # usage: sk = skill(target); sk(global_game_context)
        # fell free to define your own logics
        return self.skill_effect
    def set_owner(self, owner: Character or Beast):
        self.owner = owner
        owner.skills.append(self)

def mp_checker(func, *args, **kwargs):
    # TODO: a wrapper to check whether the skill owner's mp is enough. If not, increase self.owner.gate_armed
    pass

# The following skills used by Leprechauns and humans
class Slash(Skill):
    def __init__(self, name='斩击', description='直截了当的圣剑斩击！', owner: Character = None, **kwargs):
        super().__init__(name, description, owner)
        self.atk = kwargs.get('atk', 100)
        self.mp_cost = kwargs.get('mp_cost', 100)
        self.range = kwargs.get('range', 3)
        self.hit_rate = kwargs.get('hit_rate', 0.9)
        self.crit_rate = kwargs.get('crit_rate', 0.1)
        self.dmg_var = kwargs.get('dmg_var', 0.1)  # +-10% atk

    def __call__(self, target: Beast or Character or Ship, game:Game):
        self.owner.mp -= self.mp_cost
        if self.owner.mp >= 0:
            if np.random.random() < self.hit_rate:
                hp_damage = self.atk*((np.random.random()*2-1)*self.dmg_var + 1)
                if np.random.random() < self.crit_rate:
                    hp_damage *= 2
            return self.SkillEffect(self, hp_damage=hp_damage)
        else:
            self.owner.game.register_print(f'{self.skill.owner.name} [{self.skill.name}]\nMP: [-{self.skill.mp_cost}]{self.skill.owner.mp}\nMP不足！')
            self.owner.gate_armed += 1

    @property
    def info(self):
        return f'{self.name}: {self.description}\nATK:{self.atk}  MP_COST: {self.mp_cost}\n RANGE: {self.range}'

class Parry(Skill):
    def __init__(self, **kwargs):
        self.name = kwargs.get('name', '招架')
        self.description = kwargs.get('description', '下一回合受到攻击时抵挡一些伤害，并对射程内的敌人反击')
        self.atk = kwargs.get('atk', 0)
        self.mp_cost = kwargs.get('mp_cost', 500)
        self.range = kwargs.get('range', 2)
        self.hit_rate = kwargs.get('hit_rate', 0.9)
        self.crit_rate = kwargs.get('crit_rate', 0.1)
        self.dmg_var = kwargs.get('dmg_var', 0.1)  # variance (normal distribution)

        self.metaparam = kwargs.get('metaparam', None)  # metaparam used by __call__ for your customized skill effects

    def __call__(self, target: Beast or Character or Ship):
        # usage: sk = skill(target); sk(global_game_context)
        # fell free to define your own logics
        def execute_skill(context:Game):
            if np.random.randn() < self.hit_rate:
                damage = target.__hit__(self)  # a minus number
                # other skill logics
                return f'{self.owner.name} [{self.name}]\n>>>{target.name} {damage}({target.hp})'
            else:
                return f'{self.owner.name} [{self.name}]\n>>>{target.name} [MISS!]({target.hp})'
        return execute_skill

    def set_owner(self, owner: Character or Beast):
        self.owner = owner

    @property
    def info(self):
        return f'{self.name}: {self.description}\nATK:{self.atk}  MP_COST: {self.mp_cost}\n RANGE: {self.range}'

