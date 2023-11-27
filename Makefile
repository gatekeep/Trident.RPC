include ../build.mk

LIBS = TridentFramework.RPC.dll

.PHONY: all rebuild install clean

all: $(LIBS)
rebuild: clean all

libtrace.dll:
	$(XBUILD) $(XBUILD_ARGS) $(XBUILD_PROFILE) $(XBUILD_DYN_CONST) TridentFramework.RPC.csproj

install:
	@install -vD bin/$(PROFILE_NAME)/$(LIBS) $(libdir)
	@install -m0644 -vD bin/$(PROFILE_NAME)/TridentFramework.RPC.dll.mdb $(libdir)
clean:
	-rm -rf *dll *exe *mdb obj bin
