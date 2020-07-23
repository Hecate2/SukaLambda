from mongoengine import *

class Account(Document):
    qq = IntField(required=True, unique=True)  # qq account num

class Character(Document):  # Can be a Leprehaun or a human character
    pass

class Ship(Document):
    pass

class Beast(Document):  # one type of the 17 beasts
    pass

class Skill(Document):
    pass

