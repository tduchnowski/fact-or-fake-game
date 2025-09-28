FROM postgres:alpine
COPY ./questions.csv /tmp/
COPY ./init.sql /docker-entrypoint-initdb.d/
