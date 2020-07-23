from skill.skill import Skill, Slash
from skill.passive_skill import PassiveSkillInstructionList
from skill.effect import Effect
from character.ship import Ship
from character.torpedo import Torpedo
from game import *

default_skills = [
    Slash(atk=100, hit_rate=0.9, crit_rate=0.1, dmg_var=0.1),
]


class Character:
    skills = default_skills
    skill_instruction = skills[0]  # skill.skill.Skill, 本回合用户打算是使用的技能。如果用户没有指明本轮用什么技能，则沿用上一轮的技能
    target_instruction = None
    movement_instruction = None  # game.movement.MovementInstruction
    gate_armed = 0

    def __init__(self, game:Game, **kwargs):
        self.game = game
        self.name = kwargs.get('name', '示例人物名')

        self.hp = kwargs.get('hp', 10000)
        self.max_hp = self.hp
        self.mp = kwargs.get('mp', 2000)
        self.max_mp = kwargs.get('max_mp', self.mp)
        self.agi = kwargs.get('agi', 4)

        self.move_hit_damage = kwargs.get('move_hit_damage', 0)  # how much damage can I deal if I hit someone with my movement?
        self.move_hit_skill = Skill(name='冲撞', description='移动中冲撞其他角色造成伤害', owner=self)
        self.move_hit_effect = Effect(self.move_hit_skill, self.move_hit_damage)

        self.gate_power = kwargs.get('gate_power', 10)  # 妖精乡之门的威力最大为自身hp的几倍
        self.gate_range = kwargs.get('gate_range', float('inf'))

        self.passive_detection_dist = kwargs.get('passive_detection_dist', 4)  # 我与敌人相距多少时必定被发现
        self.active_detection_dist = kwargs.get('active_detection_dist', 3)  # 我与敌人相距多少时必定发现敌人
        # 如果我与敌人相距小于我的passive_detection_dist或者小于敌人的active_detection_dist，则我被发现

        self.passive_skill_instruction_list = PassiveSkillInstructionList(self)
        self.event_handler = {
            MyNewRound: [lambda event: self.passive_skill_instruction_list.new_round()],
            MyMovementFinish: [lambda : self.clear_movement_instruction()],
            SkillHit: [lambda event:self.__hit__(event)],
            MovementHit: [lambda event:self.move_hit_character(event.target)],
            MovementHitBy: [lambda event:self.move_hit_by_character(event.source)],
        }  # dict:{Event: [function(event), ]}

    def __repr__(self):
        return f"Character({self.info})"
    def __str__(self):
        return f"Character({self.info})"

    def handle_event(self, event:Event):
        funcs = self.event_handler.get(type(event), [])
        for func in funcs:
            func(event)

    def initialize_skills(self, skills:list[Skill]):
        '''
        set active skills for this character
        '''
        self.skills = skills
        for skill in skills:
            skill.set_owner(self)

    def set_movement_instruction(self, instruction:MovementInstruction):
        if instruction.dist > self.agi:
            self.movement_instruction = None
            return '移动距离不能大于敏捷值'
        else:
            self.movement_instruction = instruction

    def clear_movement_instruction(self):
        self.movement_instruction = None

    def set_skill_instruction(self, index:int, *args, **kwargs):
        self.skill_instruction = self.skills[index](*args, **kwargs)

    # def use_skill(self,):  # use a skill instantly

    # def remove_self_from_game(self):
    #     self.game.remove_character(self)

    def __hit__(self, event:SkillHit):
        # what happens when I am hit by a skill?
        effect = event.source_skill(self, self.game)
        if effect:
            for passive_skill in self.passive_skill_instruction_list[0]:
                effect = passive_skill(effect)
            effect(self, self.game)
            self.game.register_print(effect.gen_effect_str(self, self.game))
            if self.hp <= 0:
                self.game.register_print(f'{self.name} [FADED]')
                self.game.remove_character(self)
            if self.mp < 0:
                self.mp = 0
                self.gate_armed += 1

    def move_hit_character(self, target_character):
        target_character.__hit__(SkillHit(self.move_hit_skill, target_character))
    def move_hit_by_character(self, source_character):
        pass

    def __del__(self):
        for skill in self.skills:
            del skill
        for skill in self.passive_skill_instruction_list:
            del skill

    @property
    def info(self):
        return f'{self.name}\nHP: {self.hp}/{self.max_hp}\nMP: {self.mp}/{self.max_mp}\nAGI: {self.agi}'
    @property
    def skill_info(self):
        return 'SKILLS:\n'+'\n'.join([skill.info for skill in self.skills])