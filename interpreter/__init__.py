
from global_config import global_alias

class Compiler:
    '''
    Compiler(raw_qq_message).compile()
    '''
    def __init__(self, raw_cmd:str):
        self.raw_cmd = raw_cmd

    def __call__(self):
        self.compile()

    def compile(self):
        self.preprocess()
        return [self.cmd_to_lambda_process(cmd) for cmd in self.cmds]

    def preprocess(self):
        cmd_lines = self.raw_cmd.split('\n')  # "sudo ship info\nleft" -> ["sudo ship info", "left"]
        self.cmds = [line.split() for line in cmd_lines]  # -> ["sudo", "ship", "info"]

    class Middlewares:
        @staticmethod
        def sudo(cmd, context):
            '''
            :param cmd: ['sudo', 'ship', 'info']
            :return: A closure which prints the ship's info
            '''
            context['sudo'] = False
            while cmd[0] in global_alias['sudo']:
                context['sudo'] = True
                cmd.remove(cmd[0])
            return cmd, context

    def cmd_to_lambda_process(self, cmd):
        if not cmd:
            return lambda :()
        def execute_middlewares(cmd):
            context = {}
            cmd, context = self.Middlewares.sudo(cmd, context)
            # execute other middelwares here
            return cmd, context
        cmd, context = execute_middlewares(cmd)
        # TODO: find the callable things in the game and call them with lambda