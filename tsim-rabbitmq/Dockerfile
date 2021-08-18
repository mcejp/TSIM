FROM rabbitmq:3.9-management

COPY tsim-rabbitmq.conf /etc/rabbitmq/conf.d/tsim-rabbitmq.conf
RUN rabbitmq-plugins enable --offline rabbitmq_stomp rabbitmq_web_stomp
