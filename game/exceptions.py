

class ConditionNotSatisfied(Exception):
    pass

class NoTargetFound(Exception):
    pass

class MovementException(Exception):
    pass

class LocationConflict(MovementException):
    pass
