FROM i386/alpine
MAINTAINER James Knight <james.d.knight@live.com>

RUN apk add \
    bash \
    freetype \
    rsync \
    wine \
    --no-cache

################################################################################

# pre-build initial wine configuration
RUN WINEDEBUG=-all wine rundll32 2>/dev/null >/dev/null

################################################################################

ADD docker/bashrc     /root/.bashrc
ADD docker/entrypoint /.entrypoint

ADD LICENSES/ /usr/share/t2ds/LICENSES
ADD LICENSE   /usr/share/t2ds/
ADD GameData/ /opt/t2ds/

################################################################################

# T2DS_ARGS
# T2DS_BIND
# T2DS_INTERACTIVE
# T2DS_MOD
ENV T2DS_PORT 28000
ENV WINEDEBUG -all

EXPOSE $T2DS_PORT/UDP 23/TCP

VOLUME /opt/t2ds/base/prefs
VOLUME /opt/t2ds/classic/prefs
VOLUME /run/t2ds-overrides

################################################################################

WORKDIR /opt/t2ds
ENTRYPOINT ["/.entrypoint"]
