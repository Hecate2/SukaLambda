def func():
    class Inner:
        def __init__(self):
            print('__init__')
        def __call__(self):
            print('__call__')
    i = Inner(); return i
func()()
