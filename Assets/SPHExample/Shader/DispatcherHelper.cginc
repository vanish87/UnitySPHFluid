
int _DispatchedX;
int _DispatchedY;
int _DispatchedZ;

#define RETURN_IF_INVALID(TID) if(any(TID >= uint3(_DispatchedX, _DispatchedY,_DispatchedZ))) return;