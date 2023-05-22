from SkillVM.character import Character

class Team:
    characters = set()

    def __init__(self):
        pass
    def add_character(self, characters:Character or list[Character]):
        if type(characters) == Character:
            self.characters.add(characters)
        else:
            for character in characters:
                self.characters.add(character)
    def remove_character(self, character):
        self.characters.remove(character)