CREATE TABLE questions (
	id SERIAL PRIMARY KEY,
	text VARCHAR(255) NOT NULL,
	answer BOOLEAN NOT NULL,
	category VARCHAR(20),
	explanation VARCHAR(400)
);

CREATE INDEX category_index ON questions (category);

COPY questions (text, answer, category, explanation) FROM '/tmp/questions.csv' WITH (FORMAT csv, HEADER true, DELIMITER '|');
